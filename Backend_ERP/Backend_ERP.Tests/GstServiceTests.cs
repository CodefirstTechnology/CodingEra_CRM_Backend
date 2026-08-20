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
    public class GstServiceTests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        [Fact]
        public async Task GstService_creates_transactions_and_calculates_summary_accurately()
        {
            using var db = CreateInMemoryDbContext();
            var numbering = new GstNumberingService(db);
            var service = new GstService(db, numbering);

            // 1. Sales Invoice GST (Collected)
            var salesGst = await service.CreateGstTransactionAsync(new GstTransactionDto
            {
                InvoiceNumber = "INV-2026-101",
                CustomerName = "Reliance Infra",
                TxnType = "Sales Invoice",
                TaxableValue = 100000m,
                Cgst = 9000m,
                Sgst = 9000m,
                Igst = 0m,
                Cess = 0m,
                InvoiceDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                ReturnPeriod = "2026-03",
                Status = "Draft"
            }, "Tax User");

            Assert.NotNull(salesGst);
            Assert.StartsWith("GST-", salesGst.GstNumber);
            Assert.Equal(18000m, salesGst.TaxAmount);

            // 2. Purchase Bill GST (Input Tax Credit / Paid)
            var purchaseGst = await service.CreateGstTransactionAsync(new GstTransactionDto
            {
                InvoiceNumber = "BILL-2026-050",
                VendorName = "JSW Steel Ltd",
                TxnType = "Purchase Bill",
                TaxableValue = 50000m,
                Cgst = 4500m,
                Sgst = 4500m,
                Igst = 0m,
                Cess = 0m,
                InvoiceDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                ReturnPeriod = "2026-03",
                Status = "Draft"
            }, "Tax User");

            Assert.Equal(9000m, purchaseGst.TaxAmount);

            // 3. Verify GST Summary
            var summaryList = await service.GetGstSummaryAsync(new ListQueryDto { ReturnPeriod = "2026-03" });
            Assert.Single(summaryList);
            var sum = summaryList[0];
            Assert.Equal("2026-03", sum.ReturnPeriod);
            Assert.Equal(150000m, sum.TaxableValue);
            Assert.Equal(13500m, sum.Cgst);
            Assert.Equal(13500m, sum.Sgst);
            Assert.Equal(18000m, sum.Collected);
            Assert.Equal(9000m, sum.Paid);
            Assert.Equal(9000m, sum.Net); // 18000 - 9000
        }

        [Fact]
        public async Task GstService_transitions_return_through_verification_filing_and_closing()
        {
            using var db = CreateInMemoryDbContext();
            var numbering = new GstNumberingService(db);
            var service = new GstService(db, numbering);

            // Add transaction to trigger return creation
            await service.CreateGstTransactionAsync(new GstTransactionDto
            {
                InvoiceNumber = "INV-2026-200",
                CustomerName = "Larsen & Toubro",
                TxnType = "Sales Invoice",
                TaxableValue = 200000m,
                Cgst = 18000m,
                Sgst = 18000m,
                ReturnPeriod = "2026-04",
                Status = "Draft"
            }, "Tax Officer");

            var returns = await service.GetGstReturnsAsync(new ListQueryDto { ReturnPeriod = "2026-04" });
            Assert.Single(returns);
            var ret = returns[0];
            Assert.Equal("Draft", ret.Status);
            Assert.Equal(36000m, ret.GstCollected);

            // 1. Verify
            var verified = await service.VerifyGstReturnAsync(ret.Id, new StatusActionRequestDto { Remarks = "Tax auditor verified" }, "Auditor");
            Assert.Equal("Verified", verified.Status);

            // 2. File
            var filed = await service.FileGstReturnAsync(ret.Id, new StatusActionRequestDto { Remarks = "Filed on GST portal ARN #12345" }, "CA User");
            Assert.Equal("Filed", filed.Status);
            Assert.NotNull(filed.FiledAt);
            Assert.Equal("CA User", filed.FiledBy);

            // 3. Close
            var closed = await service.CloseGstReturnAsync(ret.Id, new StatusActionRequestDto { Remarks = "Period closed" }, "Admin");
            Assert.Equal("Closed", closed.Status);
        }

        [Fact]
        public async Task GstDashboard_aggregates_collected_paid_and_return_counts()
        {
            using var db = CreateInMemoryDbContext();
            var numbering = new GstNumberingService(db);
            var service = new GstService(db, numbering);

            await service.CreateGstTransactionAsync(new GstTransactionDto
            {
                InvoiceNumber = "INV-1",
                TxnType = "Sales Invoice",
                TaxableValue = 10000m,
                Cgst = 900m,
                Sgst = 900m,
                ReturnPeriod = "2026-05"
            }, "User");

            await service.CreateGstTransactionAsync(new GstTransactionDto
            {
                InvoiceNumber = "BILL-1",
                TxnType = "Purchase Bill",
                TaxableValue = 5000m,
                Cgst = 450m,
                Sgst = 450m,
                ReturnPeriod = "2026-05"
            }, "User");

            var dash = await service.GetGstDashboardAsync();
            Assert.Equal(1800m, dash.GstCollected);
            Assert.Equal(900m, dash.GstPaid);
            Assert.Equal(1, dash.PendingReturns);
        }
    }
}
