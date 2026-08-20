using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;
using ERP.Domain.Accounting;
using ERP.Infrastructure.Accounting;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests
{
    public class AccountingCrossModuleIntegrationAndRbacTests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        [Fact]
        public async Task RBAC_foundation_service_exposes_all_accounting_permissions()
        {
            using var db = CreateInMemoryDbContext();
            var service = new AccountingFoundationService(
                new CustomerLedgerNumberingService(db),
                new GstNumberingService(db),
                new PaymentNumberingService(db),
                new ReceiptNumberingService(db),
                new BankReconNumberingService(db));

            var perms = await service.GetPermissionsAsync();

            Assert.NotNull(perms);
            Assert.Equal(17, perms.Count);
            Assert.Contains("accounting.view", perms);
            Assert.Contains("accounting.create", perms);
            Assert.Contains("accounting.edit", perms);
            Assert.Contains("accounting.delete", perms);
            Assert.Contains("accounting.approve", perms);
            Assert.Contains("accounting.post", perms);
            Assert.Contains("accounting.customer-ledger", perms);
            Assert.Contains("accounting.gst", perms);
            Assert.Contains("accounting.payment", perms);
            Assert.Contains("accounting.receipt", perms);
            Assert.Contains("accounting.outstanding", perms);
            Assert.Contains("accounting.bank-recon", perms);
            Assert.Contains("accounting.financial-reports", perms);
            Assert.Contains("accounting.export", perms);
            Assert.Contains("accounting.print", perms);
            Assert.Contains("accounting.audit.view", perms);
            Assert.Contains("accounting.dashboard.view", perms);
        }

        [Fact]
        public async Task Sales_to_Accounting_full_cross_module_flow_updates_all_subsystems_consistently()
        {
            using var db = CreateInMemoryDbContext();

            var clNum = new CustomerLedgerNumberingService(db);
            var recNum = new ReceiptNumberingService(db);
            var gstNum = new GstNumberingService(db);
            var brNum = new BankReconNumberingService(db);

            var outService = new OutstandingService(db);
            var clService = new CustomerLedgerService(db, clNum);
            var recService = new ReceiptService(db, recNum, outService, clService);
            var gstService = new GstService(db, gstNum);
            var brService = new BankReconciliationService(db, brNum);
            var repService = new FinancialReportService(db);

            // Step 1: Sales Invoice generates Customer Ledger debit + Customer Outstanding + GST Liability
            var invoiceEntry = await clService.CreateLedgerEntryAsync(new CustomerLedgerEntryDto
            {
                CustomerId = 1001,
                CustomerName = "Godrej Aerospace",
                OpeningBalance = 0m,
                Debit = 118000m,
                Credit = 0m,
                RunningBalance = 118000m,
                Outstanding = 118000m,
                InvoiceNumber = "INV-2026-900",
                SalesOrderNumber = "SO-2026-900",
                EntryType = "Invoice",
                TransactionDate = DateTime.UtcNow.AddDays(-15)
            }, "Sales Agent");

            await outService.RecordOrUpdateOutstandingAsync(new OutstandingRowDto
            {
                PartyType = "Customer",
                PartyId = 1001,
                PartyName = "Godrej Aerospace",
                DocumentNumber = "INV-2026-900",
                OriginalAmount = 118000m,
                PaidAmount = 0m,
                Outstanding = 118000m,
                DocumentDate = DateTime.UtcNow.AddDays(-15),
                DueDate = DateTime.UtcNow.AddDays(15)
            }, "Sales Agent");

            await gstService.CreateGstTransactionAsync(new GstTransactionDto
            {
                InvoiceNumber = "INV-2026-900",
                CustomerId = 1001,
                CustomerName = "Godrej Aerospace",
                TxnType = "Sales Invoice",
                TaxableValue = 100000m,
                Cgst = 9000m,
                Sgst = 9000m,
                InvoiceDate = DateTime.UtcNow.AddDays(-15),
                ReturnPeriod = "2026-08"
            }, "Sales Agent");

            // Step 2: Receipt collected against invoice
            var receipt = await recService.CreateReceiptAsync(new ReceiptCreateRequestDto
            {
                CustomerId = 1001,
                CustomerName = "Godrej Aerospace",
                InvoiceNumber = "INV-2026-900",
                ReceiptDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ReceiptMode = "Bank Transfer",
                BankName = "HDFC Bank",
                BankAccount = "HDFC-009988",
                ReferenceNumber = "UTR-999111",
                Amount = 118000m,
                Tds = 0m
            }, "Collection User");

            await recService.ApproveReceiptAsync(receipt.Id, null, "Manager");
            await recService.PostReceiptAsync(receipt.Id, null, "Accountant");
            var received = await recService.RecordReceivedAsync(receipt.Id, new StatusActionRequestDto { Remarks = "Full payment received" }, "Cashier");

            Assert.Equal("Received", received.Status);

            // Verify Outstanding is Settled
            var customerOutstandings = await outService.GetCustomerOutstandingAsync();
            var outRow = customerOutstandings.First(o => o.DocumentNumber == "INV-2026-900");
            Assert.Equal(118000m, outRow.PaidAmount);
            Assert.Equal(0m, outRow.Outstanding);
            Assert.Equal("Settled", outRow.Status);

            // Verify Customer Ledger has both Invoice debit and Receipt credit
            var ledger = await clService.GetCustomerLedgerAsync(new ListQueryDto { CustomerId = 1001 });
            Assert.Equal(2, ledger.Count);
            var receiptEntry = ledger.First(x => x.EntryType == "Receipt");
            Assert.Equal(0m, receiptEntry.RunningBalance);

            // Step 3: Link Receipt to Bank Reconciliation
            var recon = await brService.CreateBankReconciliationAsync(new BankReconCreateRequestDto
            {
                BankName = "HDFC Bank",
                AccountNumber = "HDFC-009988",
                OpeningBalance = 1000000m,
                ClosingBalance = 1118000m,
                BookClosingBalance = 1118000m,
                ReceiptIds = new List<int> { receipt.Id }
            }, "Finance User");

            Assert.Equal(0m, recon.Difference);
            Assert.Equal(1, recon.MatchedCount);

            // Step 4: Verify Financial Reports reflect the transactions
            var pl = await repService.GetProfitLossAsync();
            Assert.True(pl.Revenue >= 118000m);

            var bs = await repService.GetBalanceSheetAsync();
            Assert.Equal(bs.Assets, bs.Liabilities + bs.Equity);
        }

        [Fact]
        public async Task Purchase_to_Accounting_full_cross_module_flow_updates_vendor_outstanding_and_reports()
        {
            using var db = CreateInMemoryDbContext();

            var payNum = new PaymentNumberingService(db);
            var gstNum = new GstNumberingService(db);
            var brNum = new BankReconNumberingService(db);

            var outService = new OutstandingService(db);
            var payService = new PaymentService(db, payNum, outService);
            var gstService = new GstService(db, gstNum);
            var brService = new BankReconciliationService(db, brNum);
            var repService = new FinancialReportService(db);

            // Step 1: Purchase Bill recorded into Vendor Outstanding + GST Input Tax Credit
            await outService.RecordOrUpdateOutstandingAsync(new OutstandingRowDto
            {
                PartyType = "Vendor",
                PartyId = 2001,
                PartyName = "Tata Steel Supplies",
                DocumentNumber = "PB-2026-999",
                OriginalAmount = 59000m,
                PaidAmount = 0m,
                Outstanding = 59000m,
                DocumentDate = DateTime.UtcNow.AddDays(-20),
                DueDate = DateTime.UtcNow.AddDays(10)
            }, "Procurement User");

            await gstService.CreateGstTransactionAsync(new GstTransactionDto
            {
                InvoiceNumber = "PB-2026-999",
                VendorId = 2001,
                VendorName = "Tata Steel Supplies",
                TxnType = "Purchase Bill",
                TaxableValue = 50000m,
                Cgst = 4500m,
                Sgst = 4500m,
                InvoiceDate = DateTime.UtcNow.AddDays(-20),
                ReturnPeriod = "2026-08"
            }, "Procurement User");

            // Step 2: Payment Entry recorded against Purchase Bill
            var payment = await payService.CreatePaymentAsync(new PaymentCreateRequestDto
            {
                VendorId = 2001,
                VendorName = "Tata Steel Supplies",
                PurchaseBillNumber = "PB-2026-999",
                PaymentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PaymentMode = "NEFT",
                BankName = "ICICI Bank",
                BankAccount = "ICICI-554433",
                ReferenceNumber = "NEFT-776655",
                Amount = 59000m,
                Tds = 0m
            }, "Accountant");

            await payService.ApprovePaymentAsync(payment.Id, null, "Manager");
            await payService.PostPaymentAsync(payment.Id, null, "Controller");
            var paid = await payService.RecordPaidAsync(payment.Id, new StatusActionRequestDto { Remarks = "Transferred via NEFT" }, "Cashier");

            Assert.Equal("Paid", paid.Status);

            // Verify Vendor Outstanding is Settled
            var vendorOutstandings = await outService.GetVendorOutstandingAsync();
            var bill = vendorOutstandings.First(b => b.DocumentNumber == "PB-2026-999");
            Assert.Equal(59000m, bill.PaidAmount);
            Assert.Equal(0m, bill.Outstanding);
            Assert.Equal("Settled", bill.Status);

            // Step 3: Link Payment to Bank Reconciliation
            var recon = await brService.CreateBankReconciliationAsync(new BankReconCreateRequestDto
            {
                BankName = "ICICI Bank",
                AccountNumber = "ICICI-554433",
                OpeningBalance = 800000m,
                ClosingBalance = 741000m,
                BookClosingBalance = 741000m,
                PaymentIds = new List<int> { payment.Id }
            }, "Finance User");

            Assert.Equal(0m, recon.Difference);
            Assert.Equal(1, recon.MatchedCount);

            // Step 4: Verify Financial Reports
            var cf = await repService.GetCashFlowAsync();
            Assert.Equal(cf.OpeningBalance + cf.Operating + cf.Investing + cf.Financing, cf.ClosingBalance);
        }
    }
}
