using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting;
using ERP.Application.Accounting.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/financial-reports")]
    public class FinancialReportsController : ControllerBase
    {
        private readonly IFinancialReportService _reportService;

        public FinancialReportsController(IFinancialReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<FinancialDashboardDto>> GetDashboard(
            [FromQuery] string? financialYear,
            [FromQuery] string? branch,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            CancellationToken cancellationToken = default)
        {
            var filter = new FinancialReportFilterDto
            {
                FinancialYear = financialYear,
                Branch = branch,
                FromDate = fromDate,
                ToDate = toDate
            };

            return Ok(await _reportService.GetFinancialDashboardAsync(filter, cancellationToken));
        }

        [HttpGet("profit-loss")]
        public async Task<ActionResult<ProfitLossReportDto>> GetProfitLoss(
            [FromQuery] string? financialYear,
            [FromQuery] string? branch,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            CancellationToken cancellationToken = default)
        {
            var filter = new FinancialReportFilterDto
            {
                FinancialYear = financialYear,
                Branch = branch,
                FromDate = fromDate,
                ToDate = toDate
            };

            return Ok(await _reportService.GetProfitLossAsync(filter, cancellationToken));
        }

        [HttpGet("balance-sheet")]
        public async Task<ActionResult<BalanceSheetReportDto>> GetBalanceSheet(
            [FromQuery] string? financialYear,
            [FromQuery] string? branch,
            [FromQuery] string? toDate,
            CancellationToken cancellationToken = default)
        {
            var filter = new FinancialReportFilterDto
            {
                FinancialYear = financialYear,
                Branch = branch,
                ToDate = toDate
            };

            return Ok(await _reportService.GetBalanceSheetAsync(filter, cancellationToken));
        }

        [HttpGet("trial-balance")]
        public async Task<ActionResult<TrialBalanceReportDto>> GetTrialBalance(
            [FromQuery] string? financialYear,
            [FromQuery] string? branch,
            [FromQuery] string? toDate,
            CancellationToken cancellationToken = default)
        {
            var filter = new FinancialReportFilterDto
            {
                FinancialYear = financialYear,
                Branch = branch,
                ToDate = toDate
            };

            return Ok(await _reportService.GetTrialBalanceAsync(filter, cancellationToken));
        }

        [HttpGet("cash-flow")]
        public async Task<ActionResult<CashFlowReportDto>> GetCashFlow(
            [FromQuery] string? financialYear,
            [FromQuery] string? branch,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            CancellationToken cancellationToken = default)
        {
            var filter = new FinancialReportFilterDto
            {
                FinancialYear = financialYear,
                Branch = branch,
                FromDate = fromDate,
                ToDate = toDate
            };

            return Ok(await _reportService.GetCashFlowAsync(filter, cancellationToken));
        }
    }
}
