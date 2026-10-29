using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IAdvancePaymentReportService
    {
        Task<AdvancePaymentAgingReportDto> GetAgingScheduleReportAsync(string tenantId = "default", CancellationToken cancellationToken = default);
        Task<Gstr1Table11ReportDto> GetGstr1Table11ReportAsync(string? monthYear = null, string tenantId = "default", CancellationToken cancellationToken = default);
    }
}
