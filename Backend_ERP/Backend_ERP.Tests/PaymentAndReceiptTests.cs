using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;
using ERP.Infrastructure.Accounting;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests
{
    public class PaymentAndReceiptTests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        [Fact]
        public async Task PaymentService_creates_validates_and_transitions_through_full_workflow()
        {
            using var db = CreateInMemoryDbContext();
            var payNum = new PaymentNumberingService(db);
            var outService = new OutstandingService(db);
            var payService = new PaymentService(db, payNum, outService);

            // Record initial vendor outstanding
            await outService.RecordOrUpdateOutstandingAsync(new OutstandingRowDto
            {
                PartyType = "Vendor",
                PartyId = 501,
                PartyName = "Tata Steel Ltd",
                DocumentNumber = "BILL-2026-100",
                OriginalAmount = 100000m,
                PaidAmount = 0m,
                Outstanding = 100000m
            }, "Admin");

            // 1. Create Draft
            var created = await payService.CreatePaymentAsync(new PaymentCreateRequestDto
            {
                VendorId = 501,
                VendorName = "Tata Steel Ltd",
                PurchaseBillId = 1,
                PurchaseBillNumber = "BILL-2026-100",
                PaymentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PaymentMode = "Bank Transfer",
                BankName = "HDFC Bank",
                BankAccount = "1234567890",
                ReferenceNumber = "UTR-999888777",
                Amount = 60000m,
                Tds = 3000m,
                Remarks = "Advance payment for raw materials"
            }, "Finance User");

            Assert.NotNull(created);
            Assert.StartsWith("PAY-", created.PaymentNumber);
            Assert.Equal("Draft", created.Status);
            Assert.Equal(57000m, created.NetAmount);

            // 2. Approve
            var approved = await payService.ApprovePaymentAsync(created.Id, new StatusActionRequestDto { Remarks = "Approved by Manager" }, "Manager");
            Assert.Equal("Approved", approved.Status);

            // 3. Post
            var posted = await payService.PostPaymentAsync(created.Id, new StatusActionRequestDto { Remarks = "Posted to GL" }, "Accountant");
            Assert.Equal("Posted", posted.Status);

            // 4. Record as Paid -> Should settle against Vendor Outstanding
            var paid = await payService.RecordPaidAsync(created.Id, new StatusActionRequestDto { Remarks = "Bank cleared" }, "Cashier");
            Assert.Equal("Paid", paid.Status);

            // Check Vendor Outstanding
            var vendorOutstandings = await outService.GetVendorOutstandingAsync();
            var bill = vendorOutstandings.First(b => b.DocumentNumber == "BILL-2026-100");
            Assert.Equal(57000m, bill.PaidAmount);
            Assert.Equal(43000m, bill.Outstanding);
            Assert.Equal("Partial", bill.Status);

            // 5. Check Dashboard
            var dash = await payService.GetPaymentDashboardAsync();
            Assert.Equal(1, dash.Paid);
        }

        [Fact]
        public async Task ReceiptService_creates_transitions_and_updates_outstanding_and_customer_ledger()
        {
            using var db = CreateInMemoryDbContext();
            var clNum = new CustomerLedgerNumberingService(db);
            var recNum = new ReceiptNumberingService(db);
            var outService = new OutstandingService(db);
            var clService = new CustomerLedgerService(db, clNum);
            var recService = new ReceiptService(db, recNum, outService, clService);

            // Seed prior customer ledger and customer outstanding
            await clService.CreateLedgerEntryAsync(new CustomerLedgerEntryDto
            {
                CustomerId = 801,
                CustomerName = "Reliance Infra",
                OpeningBalance = 0,
                Debit = 80000m,
                Credit = 0m,
                RunningBalance = 80000m,
                Outstanding = 80000m,
                InvoiceNumber = "INV-2026-500",
                EntryType = "Invoice"
            }, "Billing User");

            await outService.RecordOrUpdateOutstandingAsync(new OutstandingRowDto
            {
                PartyType = "Customer",
                PartyId = 801,
                PartyName = "Reliance Infra",
                DocumentNumber = "INV-2026-500",
                OriginalAmount = 80000m,
                PaidAmount = 0m,
                Outstanding = 80000m
            }, "Billing User");

            // 1. Create Receipt
            var receipt = await recService.CreateReceiptAsync(new ReceiptCreateRequestDto
            {
                CustomerId = 801,
                CustomerName = "Reliance Infra",
                InvoiceId = 10,
                InvoiceNumber = "INV-2026-500",
                ReceiptDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ReceiptMode = "UPI",
                BankName = "ICICI Bank",
                BankAccount = "9876543210",
                ReferenceNumber = "UPI-REF-12345",
                Amount = 50000m,
                Tds = 1000m
            }, "Collection Agent");

            Assert.NotNull(receipt);
            Assert.StartsWith("REC-", receipt.ReceiptNumber);
            Assert.Equal("Draft", receipt.Status);
            Assert.Equal(49000m, receipt.NetAmount);

            // 2. Approve & Post
            await recService.ApproveReceiptAsync(receipt.Id, null, "Manager");
            await recService.PostReceiptAsync(receipt.Id, null, "Accountant");

            // 3. Record as Received -> Triggers Outstanding settlement AND Customer Ledger entry!
            var received = await recService.RecordReceivedAsync(receipt.Id, new StatusActionRequestDto { Remarks = "Funds received in ICICI account" }, "Cashier");
            Assert.Equal("Received", received.Status);

            // Check Customer Outstanding updated
            var custOutstandings = await outService.GetCustomerOutstandingAsync();
            var outRow = custOutstandings.First(o => o.DocumentNumber == "INV-2026-500");
            Assert.Equal(49000m, outRow.PaidAmount);
            Assert.Equal(31000m, outRow.Outstanding);
            Assert.Equal("Partial", outRow.Status);

            // Check Customer Ledger updated with Credit entry & running balance recalculated
            var ledger = await clService.GetCustomerLedgerAsync(new ListQueryDto { CustomerId = 801 });
            Assert.Equal(2, ledger.Count);
            var creditEntry = ledger.First(l => l.EntryType == "Receipt");
            Assert.Equal(49000m, creditEntry.Credit);
            Assert.Equal(31000m, creditEntry.RunningBalance);
        }

        [Fact]
        public async Task PaymentService_validation_rejects_invalid_inputs()
        {
            using var db = CreateInMemoryDbContext();
            var payNum = new PaymentNumberingService(db);
            var outService = new OutstandingService(db);
            var payService = new PaymentService(db, payNum, outService);

            // Zero/Negative amount
            await Assert.ThrowsAsync<InvalidOperationException>(() => payService.CreatePaymentAsync(new PaymentCreateRequestDto
            {
                VendorId = 1,
                VendorName = "Test",
                PurchaseBillNumber = "BILL-1",
                BankName = "Bank",
                BankAccount = "123",
                PaymentMode = "Cash",
                Amount = 0m
            }, "User"));

            // TDS exceeds amount
            await Assert.ThrowsAsync<InvalidOperationException>(() => payService.CreatePaymentAsync(new PaymentCreateRequestDto
            {
                VendorId = 1,
                VendorName = "Test",
                PurchaseBillNumber = "BILL-1",
                BankName = "Bank",
                BankAccount = "123",
                PaymentMode = "Cash",
                Amount = 1000m,
                Tds = 1500m
            }, "User"));

            // Cheque without cheque number
            await Assert.ThrowsAsync<InvalidOperationException>(() => payService.CreatePaymentAsync(new PaymentCreateRequestDto
            {
                VendorId = 1,
                VendorName = "Test",
                PurchaseBillNumber = "BILL-1",
                BankName = "Bank",
                BankAccount = "123",
                PaymentMode = "Cheque",
                Amount = 1000m,
                ChequeNumber = null
            }, "User"));
        }
    }
}
