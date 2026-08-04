using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IProformaInvoiceService
    {
        Task<IReadOnlyList<ProformaInvoiceListItemDto>> GetAllAsync(
            ProformaInvoiceListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<ProformaInvoiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<ProformaInvoiceDto> CreateAsync(
            ProformaInvoiceCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<ProformaInvoiceDto?> UpdateAsync(
            int id,
            ProformaInvoiceUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<ProformaInvoiceDto?> DuplicateAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<ProformaInvoiceDto?> UpdateStatusAsync(
            int id,
            ProformaInvoiceStatusUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<ProformaInvoiceDto?> ApplyApprovalAsync(
            int id,
            ProformaInvoiceApprovalRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<ProformaInvoiceDto?> ConvertAsync(
            int id,
            ProformaInvoiceConvertRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProformaInvoiceStatusHistoryDto>?> GetStatusHistoryAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProformaInvoiceApprovalHistoryDto>?> GetApprovalHistoryAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<ProformaDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

        Task<ProformaReportResultDto> GetReportsAsync(
            ProformaInvoiceListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<ProformaExportMetadataDto> ExportReportsAsync(
            ProformaExportRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ProformaPdfResultDto?> GeneratePdfAsync(int id, CancellationToken cancellationToken = default);

        Task<ProformaEmailResultDto?> SendEmailAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetPermissionsAsync();

        Task<IReadOnlyList<ProformaLookupCustomerDto>> LookupCustomersAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProformaLookupSalesOrderDto>> LookupSalesOrdersAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProformaLookupQuotationDto>> LookupQuotationsAsync(
            CancellationToken cancellationToken = default);
    }
}
