using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IVendorReportService
    {
        Task<IReadOnlyList<LeadTimeVarianceReportRowDto>> GetLeadTimeVarianceReportAsync(
            VendorReportFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<CategorySpendConcentrationReportDto> GetCategorySpendConcentrationReportAsync(
            VendorReportFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<MsmePaymentDueAuditRowDto>> GetMsmePaymentDueAuditReportAsync(
            VendorReportFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<QcDefectParetoReportDto> GetQcDefectParetoReportAsync(
            VendorReportFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<byte[]> ExportReportCsvAsync(
            string reportType,
            VendorReportFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<VendorReportResultDto> GetVendorReportResultAsync(
            VendorReportFilterFrontendRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
