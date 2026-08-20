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
    public class FinancialReportsTests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        [Fact]
        public async Task ProfitAndLoss_calculates_gross_and_net_profit_correctly()
        {
            using var db = CreateInMemoryDbContext();
            var service = new FinancialReportService(db);

            var pl = await service.GetProfitLossAsync(new FinancialReportFilterDto
            {
                FinancialYear = "FY 2026-27",
                Branch = "Head Office"
            });

            Assert.NotNull(pl);
            Assert.Equal("FY 2026-27", pl.FinancialYear);
            Assert.Equal("Head Office", pl.Branch);
            Assert.True(pl.Revenue > 0);
            Assert.True(pl.Expenses > 0);
            Assert.Equal(pl.Revenue - pl.ExpenseLines.First(x => x.Label == "Purchase Cost").Amount, pl.GrossProfit);
            Assert.Equal(pl.Revenue - pl.Expenses, pl.NetProfit);
        }

        [Fact]
        public async Task BalanceSheet_satisfies_fundamental_accounting_equation()
        {
            using var db = CreateInMemoryDbContext();
            var service = new FinancialReportService(db);

            var bs = await service.GetBalanceSheetAsync(new FinancialReportFilterDto
            {
                FinancialYear = "FY 2026-27"
            });

            Assert.NotNull(bs);
            Assert.True(bs.Assets > 0);
            Assert.True(bs.Liabilities > 0);
            Assert.True(bs.Equity > 0);

            // Assets == Liabilities + Equity
            Assert.Equal(bs.Assets, bs.Liabilities + bs.Equity);
        }

        [Fact]
        public async Task TrialBalance_balances_debits_and_credits_accurately()
        {
            using var db = CreateInMemoryDbContext();
            var service = new FinancialReportService(db);

            var tb = await service.GetTrialBalanceAsync(new FinancialReportFilterDto
            {
                FinancialYear = "FY 2026-27"
            });

            Assert.NotNull(tb);
            Assert.NotEmpty(tb.Lines);
            Assert.True(tb.TotalDebit > 0);
            Assert.True(tb.TotalCredit > 0);

            // TotalDebit == TotalCredit
            Assert.Equal(tb.TotalDebit, tb.TotalCredit);
        }

        [Fact]
        public async Task CashFlow_calculates_opening_operating_investing_financing_and_closing_cash()
        {
            using var db = CreateInMemoryDbContext();
            var service = new FinancialReportService(db);

            var cf = await service.GetCashFlowAsync(new FinancialReportFilterDto
            {
                FinancialYear = "FY 2026-27"
            });

            Assert.NotNull(cf);
            Assert.NotEmpty(cf.Lines);
            Assert.Equal(cf.OpeningBalance + cf.Operating + cf.Investing + cf.Financing, cf.ClosingBalance);
        }

        [Fact]
        public async Task FinancialDashboard_aligns_with_pl_and_cash_flow_metrics()
        {
            using var db = CreateInMemoryDbContext();
            var service = new FinancialReportService(db);

            var dash = await service.GetFinancialDashboardAsync();
            var pl = await service.GetProfitLossAsync();
            var cf = await service.GetCashFlowAsync();

            Assert.Equal(pl.Revenue, dash.Revenue);
            Assert.Equal(pl.Expenses, dash.Expenses);
            Assert.Equal(pl.NetProfit, dash.Profit);
            Assert.Equal(cf.ClosingBalance, dash.Cash);
        }
    }
}
