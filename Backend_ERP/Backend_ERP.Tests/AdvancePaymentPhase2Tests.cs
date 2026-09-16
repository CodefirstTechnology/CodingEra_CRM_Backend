using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests
{
    public class AdvancePaymentPhase2Tests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        private (AdvancePaymentService Service, ERPDbContext Db) CreateService(ERPDbContext db)
        {
            var repo = new AdvancePaymentRepository(db);
            var numbering = new AdvancePaymentNumberingService(db);
            var service = new AdvancePaymentService(repo, numbering, db);
            return (service, db);
        }

        // ─── TEST 1: Statutory GST Receipt Voucher Generation ─────────────────────
        [Fact]
        public async Task Test_Statutory_Receipt_Voucher_Generation()
        {
            using var db = CreateInMemoryDbContext();
            var (service, _) = CreateService(db);

            var adv = new AdvancePayment
            {
                Id = 301,
                PaymentNumber = "ADV-2026-0301",
                CustomerId = "10",
                CustomerName = "Maharashtra Industrial Corp",
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PaymentMode = AdvancePaymentModes.NEFT,
                Currency = "INR",
                ExchangeRate = 1m,
                AdvanceAmount = 118000m,
                AppliedAmount = 0m,
                RemainingAmount = 118000m,
                PlaceOfSupply = "Maharashtra",
                Status = AdvancePaymentStatuses.FinanceVerification
            };
            db.AdvancePayments.Add(adv);
            await db.SaveChangesAsync();

            // Receive advance with Intra-State Place of Supply
            var receiveDto = new AdvancePaymentReceiveRequestDto
            {
                PlaceOfSupply = "Maharashtra",
                IsInterState = false,
                Remarks = "Payment verified and receipt confirmed via RTGS"
            };
            var result = await service.ReceiveAsync(301, receiveDto, "FinanceOfficer");

            Assert.NotNull(result);
            Assert.Equal(AdvancePaymentStatuses.Received, result.Status);

            var voucher = await db.AdvancePaymentReceiptVouchers
                .FirstOrDefaultAsync(v => v.AdvancePaymentId == 301);

            Assert.NotNull(voucher);
            Assert.StartsWith("RV-", voucher.VoucherNumber);
            Assert.False(voucher.IsInterState);
            Assert.Equal("Maharashtra", voucher.PlaceOfSupply);
            Assert.Equal(100000m, voucher.TaxableAmount);
            Assert.Equal(9.00m, voucher.CgstRate);
            Assert.Equal(9000m, voucher.CgstAmount);
            Assert.Equal(9.00m, voucher.SgstRate);
            Assert.Equal(9000m, voucher.SgstAmount);
            Assert.Equal(0m, voucher.IgstRate);
            Assert.Equal(0m, voucher.IgstAmount);
            Assert.Equal(118000m, voucher.TotalVoucherAmount);
            Assert.Equal("FinanceOfficer", voucher.CreatedBy);
        }

        // ─── TEST 2: Statutory GST Refund Voucher Generation ──────────────────────
        [Fact]
        public async Task Test_Statutory_Refund_Voucher_Generation()
        {
            using var db = CreateInMemoryDbContext();
            var (service, _) = CreateService(db);

            var adv = new AdvancePayment
            {
                Id = 302,
                PaymentNumber = "ADV-2026-0302",
                CustomerId = "11",
                CustomerName = "Mumbai Tech Solutions",
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PaymentMode = AdvancePaymentModes.RTGS,
                Currency = "INR",
                ExchangeRate = 1m,
                AdvanceAmount = 118000m,
                AppliedAmount = 0m,
                RemainingAmount = 118000m,
                PlaceOfSupply = "Maharashtra",
                Status = AdvancePaymentStatuses.FinanceVerification
            };
            db.AdvancePayments.Add(adv);
            await db.SaveChangesAsync();

            // 1. Confirm Receipt -> creates Receipt Voucher (Taxable 100k, CGST 9k, SGST 9k, Total Tax 18k)
            await service.ReceiveAsync(302, new AdvancePaymentReceiveRequestDto
            {
                PlaceOfSupply = "Maharashtra",
                IsInterState = false,
                Remarks = "Received full advance"
            }, "FinanceUser");

            var receiptVoucher = await db.AdvancePaymentReceiptVouchers
                .FirstAsync(v => v.AdvancePaymentId == 302);
            Assert.NotNull(receiptVoucher);

            // 2. Refund 59,000 (50% of the advance amount)
            var refundReq = new ProcessRefundRequestDto
            {
                RefundAmount = 59000m,
                RefundReferenceNumber = "UTR-REF-202609-001",
                Remarks = "Partial refund requested by customer before allocation"
            };
            var refundResult = await service.ProcessRefundAsync(302, refundReq, "FinanceUser");

            Assert.NotNull(refundResult);
            Assert.Equal(59000m, refundResult.RefundedAmount);
            Assert.Equal(59000m, refundResult.RemainingAmount);

            var refundVoucher = await db.AdvancePaymentRefundVouchers
                .FirstOrDefaultAsync(r => r.AdvancePaymentId == 302);

            Assert.NotNull(refundVoucher);
            Assert.StartsWith("RFV-", refundVoucher.RefundVoucherNumber);
            Assert.Equal(receiptVoucher.Id, refundVoucher.ReceiptVoucherId);
            Assert.Equal(59000m, refundVoucher.RefundAmount);
            // Tax refunded is pro-rated: 59000 * (18000 / 118000) = 9000
            Assert.Equal(9000m, refundVoucher.TaxRefundedAmount);
            Assert.Equal("UTR-REF-202609-001", refundVoucher.BankReferenceNumber);
            Assert.Equal("FinanceUser", refundVoucher.CreatedBy);
        }

        // ─── TEST 3: Proforma Invoice Auto-Settlement on Advance Allocation ──────
        [Fact]
        public async Task Test_Proforma_Invoice_AutoSettlement()
        {
            using var db = CreateInMemoryDbContext();
            var (service, _) = CreateService(db);

            var so = new SalesOrder
            {
                Id = 303,
                SalesOrderNumber = "SO-2026-0303",
                CustomerName = "Apex Engineering",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Subtotal = 100000m,
                GrandTotal = 100000m,
                AdvanceAllocatedAmount = 0m,
                Status = SalesOrderStatuses.Confirmed,
                Remarks = "Payment Hold pending advance receipt"
            };
            db.SalesOrders.Add(so);

            var pi = new ProformaInvoice
            {
                Id = 401,
                PiNumber = "PI-2026-0401",
                SalesOrderId = 303,
                SalesOrderNumber = "SO-2026-0303",
                CustomerName = "Apex Engineering",
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                GrandTotal = 100000m,
                AdvanceReceivedAmount = 0m,
                PaymentStatus = "Unpaid",
                Status = ProformaInvoiceStatuses.Sent
            };
            db.ProformaInvoices.Add(pi);

            var adv = new AdvancePayment
            {
                Id = 303,
                PaymentNumber = "ADV-2026-0303",
                CustomerId = "12",
                CustomerName = "Apex Engineering",
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PaymentMode = AdvancePaymentModes.NEFT,
                Currency = "INR",
                AdvanceAmount = 100000m,
                AppliedAmount = 0m,
                RemainingAmount = 100000m,
                Status = AdvancePaymentStatuses.Received
            };
            db.AdvancePayments.Add(adv);
            await db.SaveChangesAsync();

            // 1. Apply first tranche: 50,000
            var apply1 = new AdvancePaymentApplyRequestDto
            {
                SalesOrderId = 303,
                SalesOrderNumber = "SO-2026-0303",
                ApplyAmount = 50000m,
                Remarks = "Tranche 1 allocation"
            };
            await service.ApplyAsync(303, apply1, "Accountant1");

            var updatedPi1 = await db.ProformaInvoices.FindAsync(401);
            Assert.NotNull(updatedPi1);
            Assert.Equal(50000m, updatedPi1.AdvanceReceivedAmount);
            Assert.Equal("PartiallyPaid", updatedPi1.PaymentStatus);

            // 2. Apply second tranche: remaining 50,000
            var apply2 = new AdvancePaymentApplyRequestDto
            {
                SalesOrderId = 303,
                SalesOrderNumber = "SO-2026-0303",
                ApplyAmount = 50000m,
                Remarks = "Tranche 2 final allocation"
            };
            await service.ApplyAsync(303, apply2, "Accountant1");

            var updatedPi2 = await db.ProformaInvoices.FindAsync(401);
            Assert.NotNull(updatedPi2);
            Assert.Equal(100000m, updatedPi2.AdvanceReceivedAmount);
            Assert.Equal("FullyPaid", updatedPi2.PaymentStatus);

            var updatedSo = await db.SalesOrders.FindAsync(303);
            Assert.NotNull(updatedSo);
            Assert.Equal(100000m, updatedSo.AdvanceAllocatedAmount);
            Assert.Contains("Payment Cleared", updatedSo.Remarks);
        }

        // ─── TEST 4: Realized FX Gain/Loss Differential ───────────────────────────
        [Fact]
        public async Task Test_Realized_FX_Gain_Loss_Differential()
        {
            using var db = CreateInMemoryDbContext();
            var (service, _) = CreateService(db);

            var so = new SalesOrder
            {
                Id = 304,
                SalesOrderNumber = "SO-2026-0304",
                CustomerName = "US Global Corp",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Subtotal = 1000m,
                GrandTotal = 1000m,
                AdvanceAllocatedAmount = 0m,
                Status = SalesOrderStatuses.Confirmed
            };
            db.SalesOrders.Add(so);

            // USD 1,000 booked at ₹83.00
            var adv = new AdvancePayment
            {
                Id = 304,
                PaymentNumber = "ADV-2026-0304",
                CustomerId = "13",
                CustomerName = "US Global Corp",
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                PaymentMode = AdvancePaymentModes.BankTransfer,
                Currency = "USD",
                ExchangeRate = 83.00m,
                AdvanceAmount = 1000m,
                AppliedAmount = 0m,
                RemainingAmount = 1000m,
                Status = AdvancePaymentStatuses.Received
            };
            db.AdvancePayments.Add(adv);
            await db.SaveChangesAsync();

            // Allocation occurs when USD rate has appreciated to ₹84.50
            var applyReq = new AdvancePaymentApplyRequestDto
            {
                SalesOrderId = 304,
                SalesOrderNumber = "SO-2026-0304",
                ApplyAmount = 1000m,
                ExchangeRateAtAllocation = 84.50m,
                Remarks = "Full allocation in USD with updated spot rate"
            };

            var result = await service.ApplyAsync(304, applyReq, "TreasuryManager");
            Assert.NotNull(result);

            var app = await db.AdvancePaymentApplications
                .FirstAsync(a => a.AdvancePaymentId == 304);

            Assert.Equal(84.50m, app.ExchangeRateAtAllocation);
            // Realized FX = 1000 * (84.50 - 83.00) = +1500.00 (Gain)
            Assert.Equal(1500.00m, app.RealizedFxGainLoss);

            // Assert timeline logs the realized FX gain
            var timeline = await db.AdvancePaymentTimelines
                .Where(t => t.AdvancePaymentId == 304 && t.Remarks.Contains("FX Realization"))
                .ToListAsync();

            Assert.NotEmpty(timeline);
            var fxEvent = timeline.First();
            Assert.Contains("Gain", fxEvent.Remarks);
            Assert.Contains("1,500.00", fxEvent.Remarks);
            Assert.Contains("84.5000", fxEvent.Remarks);
            Assert.Contains("83.0000", fxEvent.Remarks);
        }
    }
}
