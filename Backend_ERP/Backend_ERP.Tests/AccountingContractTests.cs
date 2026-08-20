using System;
using System.Text.Json;
using System.Threading.Tasks;
using ERP.Application.Accounting;
using ERP.Application.Accounting.Dtos;
using ERP.Domain.Accounting;
using ERP.Infrastructure.Accounting;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests
{
    public class AccountingContractTests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        [Fact]
        public void AccountingPermissions_contains_all_seventeen_frontend_permissions()
        {
            var perms = AccountingPermissions.All;

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
            Assert.Contains("accounting.dashboard.view", perms);
            Assert.Contains("accounting.export", perms);
            Assert.Contains("accounting.print", perms);
            Assert.Contains("accounting.audit.view", perms);
        }

        [Fact]
        public async Task AccountingFoundationService_returns_all_rbac_permissions()
        {
            using var db = CreateInMemoryDbContext();
            var clNum = new CustomerLedgerNumberingService(db);
            var gstNum = new GstNumberingService(db);
            var payNum = new PaymentNumberingService(db);
            var recNum = new ReceiptNumberingService(db);
            var brsNum = new BankReconNumberingService(db);
            var service = new AccountingFoundationService(clNum, gstNum, payNum, recNum, brsNum);

            var perms = await service.GetPermissionsAsync();
            Assert.Equal(17, perms.Count);
        }

        [Fact]
        public async Task AccountingNumberingServices_generate_expected_prefixes()
        {
            using var db = CreateInMemoryDbContext();
            var clNum = new CustomerLedgerNumberingService(db);
            var gstNum = new GstNumberingService(db);
            var payNum = new PaymentNumberingService(db);
            var recNum = new ReceiptNumberingService(db);
            var brsNum = new BankReconNumberingService(db);

            var cl = await clNum.GenerateNumberAsync();
            var gst = await gstNum.GenerateNumberAsync();
            var pay = await payNum.GenerateNumberAsync();
            var rec = await recNum.GenerateNumberAsync();
            var brs = await brsNum.GenerateNumberAsync();

            var currentYear = DateTime.UtcNow.Year;
            Assert.StartsWith($"CL-{currentYear}-001", cl);
            Assert.StartsWith($"GST-{currentYear}-001", gst);
            Assert.StartsWith($"PAY-{currentYear}-001", pay);
            Assert.StartsWith($"REC-{currentYear}-001", rec);
            Assert.StartsWith($"BRS-{currentYear}-001", brs);
        }

        [Fact]
        public void CustomerLedgerDto_exposes_expected_frontend_properties()
        {
            var dto = new CustomerLedgerEntryDto
            {
                Id = 1,
                LedgerNumber = "CL-2026-001",
                CustomerId = 10,
                CustomerName = "Acme Corp",
                OpeningBalance = 1000m,
                Debit = 500m,
                Credit = 200m,
                RunningBalance = 1300m,
                Outstanding = 1300m,
                EntryType = "Invoice",
                AgeingBucket = "0-30"
            };

            Assert.Equal("CL-2026-001", dto.LedgerNumber);
            Assert.Equal("Acme Corp", dto.CustomerName);
            Assert.Equal(1300m, dto.RunningBalance);
            Assert.Equal("Invoice", dto.EntryType);
            Assert.Equal("0-30", dto.AgeingBucket);
        }

        [Fact]
        public void PaymentEntryDto_calculates_and_serializes_cleanly()
        {
            var dto = new PaymentEntryDto
            {
                Id = 1,
                PaymentNumber = "PAY-2026-001",
                VendorId = 5,
                VendorName = "Steel Industries Ltd",
                PurchaseBillNumber = "BILL-2026-000001",
                PaymentMode = "Bank Transfer",
                BankName = "HDFC Bank",
                Amount = 10000m,
                Tds = 500m,
                NetAmount = 9500m,
                Status = "Approved"
            };

            var json = JsonSerializer.Serialize(dto);
            Assert.Contains("PAY-2026-001", json);
            Assert.Contains("Steel Industries Ltd", json);
            Assert.Contains("9500", json);
        }

        [Fact]
        public void ReceiptEntryDto_serializes_cleanly()
        {
            var dto = new ReceiptEntryDto
            {
                Id = 1,
                ReceiptNumber = "REC-2026-001",
                CustomerId = 10,
                CustomerName = "Acme Corp",
                InvoiceNumber = "INV-2026-001",
                ReceiptMode = "UPI",
                BankName = "ICICI Bank",
                Amount = 5000m,
                Tds = 100m,
                NetAmount = 4900m,
                Status = "Received"
            };

            var json = JsonSerializer.Serialize(dto);
            Assert.Contains("REC-2026-001", json);
            Assert.Contains("Acme Corp", json);
            Assert.Contains("4900", json);
        }
    }
}
