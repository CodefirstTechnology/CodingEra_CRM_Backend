using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;

namespace ERP.Application.Accounting
{
    public interface IFinancialReportService
    {
        Task<ProfitLossReportDto> GetProfitLossAsync(FinancialReportFilterDto? filter = null, CancellationToken cancellationToken = default);
        Task<BalanceSheetReportDto> GetBalanceSheetAsync(FinancialReportFilterDto? filter = null, CancellationToken cancellationToken = default);
        Task<TrialBalanceReportDto> GetTrialBalanceAsync(FinancialReportFilterDto? filter = null, CancellationToken cancellationToken = default);
        Task<CashFlowReportDto> GetCashFlowAsync(FinancialReportFilterDto? filter = null, CancellationToken cancellationToken = default);
        Task<FinancialDashboardDto> GetFinancialDashboardAsync(FinancialReportFilterDto? filter = null, CancellationToken cancellationToken = default);
    }
}
