using System;
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
    public class CustomerLedgerAndOutstandingTests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        [Fact]
        public async Task CustomerLedger_creates_entry_and_calculates_running_balance_correctly()
        {
            using var db = CreateInMemoryDbContext();
            var numbering = new CustomerLedgerNumberingService(db);
            var service = new CustomerLedgerService(db, numbering);

            var entry1 = await service.CreateLedgerEntryAsync(new CustomerLedgerEntryDto
            {
                CustomerId = 101,
                CustomerName = "Apex Industries",
                OpeningBalance = 0,
                Debit = 50000m,
                Credit = 0,
                RunningBalance = 50000m,
                Outstanding = 50000m,
                InvoiceNumber = "INV-2026-001",
                EntryType = "Invoice",
                TransactionDate = DateTime.UtcNow.AddDays(-10)
            }, "Test User");

            Assert.NotNull(entry1);
            Assert.StartsWith("CL-", entry1.LedgerNumber);
            Assert.Equal(50000m, entry1.RunningBalance);

            var entry2 = await service.CreateLedgerEntryAsync(new CustomerLedgerEntryDto
            {
                CustomerId = 101,
                CustomerName = "Apex Industries",
                OpeningBalance = 50000m,
                Debit = 0,
                Credit = 20000m,
                RunningBalance = 30000m,
                Outstanding = 30000m,
                ReceiptNumber = "REC-2026-001",
                EntryType = "Receipt",
                TransactionDate = DateTime.UtcNow
            }, "Test User");

            Assert.Equal(30000m, entry2.RunningBalance);

            var list = await service.GetCustomerLedgerAsync(new ListQueryDto { CustomerId = 101 });
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task CustomerStatement_calculates_totals_and_overdue_correctly()
        {
            using var db = CreateInMemoryDbContext();
            var numbering = new CustomerLedgerNumberingService(db);
            var service = new CustomerLedgerService(db, numbering);

            await service.CreateLedgerEntryAsync(new CustomerLedgerEntryDto
            {
                CustomerId = 102,
                CustomerName = "Bharat Electronics",
                OpeningBalance = 10000m,
                Debit = 40000m,
                Credit = 0,
                RunningBalance = 50000m,
                Outstanding = 40000m,
                InvoiceNumber = "INV-2026-010",
                EntryType = "Invoice",
                AgeingDays = 45, // Overdue (> 30 days)
                TransactionDate = DateTime.UtcNow.AddDays(-45)
            }, "Test User");

            await service.CreateLedgerEntryAsync(new CustomerLedgerEntryDto
            {
                CustomerId = 102,
                CustomerName = "Bharat Electronics",
                OpeningBalance = 50000m,
                Debit = 0,
                Credit = 15000m,
                RunningBalance = 35000m,
                Outstanding = 25000m,
                ReceiptNumber = "REC-2026-010",
                EntryType = "Receipt",
                AgeingDays = 0,
                TransactionDate = DateTime.UtcNow
            }, "Test User");

            var statement = await service.GetCustomerStatementAsync(102);
            Assert.NotNull(statement);
            Assert.Equal(10000m, statement.Summary.OpeningBalance);
            Assert.Equal(40000m, statement.Summary.TotalDebit);
            Assert.Equal(15000m, statement.Summary.TotalCredit);
            Assert.Equal(35000m, statement.Summary.ClosingBalance);
            Assert.Equal(40000m, statement.Summary.Overdue);
            Assert.Equal(2, statement.Entries.Count);
        }

        [Fact]
        public async Task OutstandingService_records_and_calculates_ageing_buckets_accurately()
        {
            using var db = CreateInMemoryDbContext();
            var service = new OutstandingService(db);

            // Record 1: Current 0-30 days
            await service.RecordOrUpdateOutstandingAsync(new OutstandingRowDto
            {
                PartyType = "Customer",
                PartyId = 201,
                PartyName = "Delta Systems",
                DocumentNumber = "INV-2026-101",
                DocumentDate = DateTime.UtcNow.AddDays(-10),
                DueDate = DateTime.UtcNow.AddDays(20),
                OriginalAmount = 10000m,
                PaidAmount = 0m,
                Outstanding = 10000m
            }, "Admin");

            // Record 2: 31-60 days overdue
            await service.RecordOrUpdateOutstandingAsync(new OutstandingRowDto
            {
                PartyType = "Customer",
                PartyId = 202,
                PartyName = "Echo Logistics",
                DocumentNumber = "INV-2026-102",
                DocumentDate = DateTime.UtcNow.AddDays(-40),
                DueDate = DateTime.UtcNow.AddDays(-10),
                OriginalAmount = 25000m,
                PaidAmount = 5000m,
                Outstanding = 20000m
            }, "Admin");

            // Record 3: Vendor Payable
            await service.RecordOrUpdateOutstandingAsync(new OutstandingRowDto
            {
                PartyType = "Vendor",
                PartyId = 301,
                PartyName = "Global Steel Suppliers",
                DocumentNumber = "BILL-2026-001",
                DocumentDate = DateTime.UtcNow.AddDays(-15),
                DueDate = DateTime.UtcNow.AddDays(15),
                OriginalAmount = 80000m,
                PaidAmount = 30000m,
                Outstanding = 50000m
            }, "Admin");

            var ageing = await service.GetAgeingReportAsync(new ListQueryDto { PartyType = "Customer" });
            Assert.NotNull(ageing);
            Assert.Equal(30000m, ageing.Total);
            Assert.Equal(10000m, ageing.Bucket0to30);
            Assert.Equal(20000m, ageing.Bucket31to60);
            Assert.Equal(0m, ageing.Bucket61to90);
            Assert.Equal(0m, ageing.Bucket90plus);

            var dashboard = await service.GetOutstandingDashboardAsync();
            Assert.Equal(30000m, dashboard.CustomerOutstanding);
            Assert.Equal(50000m, dashboard.VendorOutstanding);
            Assert.Equal(80000m, dashboard.TotalOutstanding);
            Assert.Equal(20000m, dashboard.Overdue);
        }

        [Fact]
        public async Task OutstandingService_settlement_updates_paid_amount_and_status()
        {
            using var db = CreateInMemoryDbContext();
            var service = new OutstandingService(db);

            await service.RecordOrUpdateOutstandingAsync(new OutstandingRowDto
            {
                PartyType = "Customer",
                PartyId = 205,
                PartyName = "Foxtrot Enterprises",
                DocumentNumber = "INV-2026-200",
                DocumentDate = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow.AddDays(25),
                OriginalAmount = 50000m,
                PaidAmount = 0m,
                Outstanding = 50000m
            }, "Admin");

            // Partial settlement
            var settledPartial = await service.ApplySettlementAsync("Customer", "INV-2026-200", 20000m);
            Assert.True(settledPartial);

            var rows = await service.GetCustomerOutstandingAsync();
            var row = rows.First(r => r.DocumentNumber == "INV-2026-200");
            Assert.Equal(20000m, row.PaidAmount);
            Assert.Equal(30000m, row.Outstanding);
            Assert.Equal("Partial", row.Status);

            // Full settlement
            var settledFull = await service.ApplySettlementAsync("Customer", "INV-2026-200", 30000m);
            Assert.True(settledFull);

            rows = await service.GetCustomerOutstandingAsync();
            row = rows.First(r => r.DocumentNumber == "INV-2026-200");
            Assert.Equal(50000m, row.PaidAmount);
            Assert.Equal(0m, row.Outstanding);
            Assert.Equal("Settled", row.Status);
        }
    }
}
