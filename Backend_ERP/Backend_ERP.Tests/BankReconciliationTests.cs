using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;
using ERP.Infrastructure.Accounting;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests
{
    public class BankReconciliationTests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        [Fact]
        public async Task BankReconciliation_creates_calculates_difference_and_transitions_workflow()
        {
            using var db = CreateInMemoryDbContext();
            var numbering = new BankReconNumberingService(db);
            var service = new BankReconciliationService(db, numbering);

            // 1. Create
            var created = await service.CreateBankReconciliationAsync(new BankReconCreateRequestDto
            {
                BankName = "State Bank of India",
                AccountNumber = "SBI-9876543210",
                StatementDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                OpeningBalance = 500000m,
                ClosingBalance = 750000m,
                BookClosingBalance = 720000m,
                PaymentIds = new List<int> { 1, 2 },
                ReceiptIds = new List<int> { 10 },
                Remarks = "Monthly bank reconciliation"
            }, "Finance User");

            Assert.NotNull(created);
            Assert.StartsWith("BRS-", created.ReconciliationNumber);
            Assert.Equal("Draft", created.Status);
            Assert.Equal(30000m, created.Difference); // |750000 - 720000|
            Assert.Equal(3, created.MatchedCount);    // 2 payments + 1 receipt
            Assert.Equal(1, created.UnmatchedCount);  // Difference > 0

            // 2. Verify
            var verified = await service.VerifyBankReconciliationAsync(created.Id, new StatusActionRequestDto { Remarks = "Verified with statement" }, "Auditor");
            Assert.Equal("Verified", verified.Status);

            // 3. Reconcile (requires remarks)
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReconcileBankAsync(created.Id, new StatusActionRequestDto { Remarks = "" }, "Manager"));

            var reconciled = await service.ReconcileBankAsync(created.Id, new StatusActionRequestDto { Remarks = "Cleared items reconciled" }, "Manager");
            Assert.Equal("Reconciled", reconciled.Status);

            // 4. Close (requires remarks)
            var closed = await service.CloseBankReconciliationAsync(created.Id, new StatusActionRequestDto { Remarks = "Reconciliation closed for March" }, "Controller");
            Assert.Equal("Closed", closed.Status);

            // 5. Dashboard
            var dash = await service.GetBankReconciliationDashboardAsync();
            Assert.Equal(3, dash.Matched);
            Assert.Equal(0, dash.Pending); // Since it's closed
            Assert.Equal(30000m, dash.Difference);
        }

        [Fact]
        public async Task BankReconciliation_update_recalculates_difference_and_matched_counts()
        {
            using var db = CreateInMemoryDbContext();
            var numbering = new BankReconNumberingService(db);
            var service = new BankReconciliationService(db, numbering);

            var created = await service.CreateBankReconciliationAsync(new BankReconCreateRequestDto
            {
                BankName = "HDFC Bank",
                AccountNumber = "HDFC-11223344",
                OpeningBalance = 100000m,
                ClosingBalance = 200000m,
                BookClosingBalance = 150000m
            }, "User");

            Assert.Equal(50000m, created.Difference);

            var updated = await service.UpdateBankReconciliationAsync(created.Id, new BankReconUpdateRequestDto
            {
                BookClosingBalance = 200000m,
                PaymentIds = new List<int> { 5, 6, 7 }
            }, "User");

            Assert.Equal(0m, updated.Difference);
            Assert.Equal(0, updated.UnmatchedCount);
            Assert.Equal(3, updated.MatchedCount);
        }
    }
}
