using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IAdvancePaymentService
    {
        Task<IReadOnlyList<AdvancePaymentListItemDto>> GetAllAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<AdvancePaymentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<AdvancePaymentDto> CreateAsync(
            AdvancePaymentCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<AdvancePaymentDto?> UpdateAsync(
            int id,
            AdvancePaymentUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<AdvancePaymentDto?> SubmitAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<AdvancePaymentDto?> VerifyAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<AdvancePaymentDto?> ReceiveAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<AdvancePaymentDto?> RejectAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<AdvancePaymentDto?> CancelAsync(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<AdvancePaymentDto?> ApplyAsync(
            int id,
            AdvancePaymentApplyRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdvancePaymentTimelineDto>?> GetTimelineAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdvancePaymentLedgerDto>> GetLedgerAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<AdvancePaymentDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

        Task<AdvancePaymentReportDto> GetReportsAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<AdvancePaymentExportMetadataDto> ExportReportsAsync(
            AdvancePaymentExportRequestDto request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetPermissionsAsync();

        Task<IReadOnlyList<AdvancePaymentLookupCustomerDto>> LookupCustomersAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdvancePaymentLookupSalesOrderDto>> LookupSalesOrdersAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdvancePaymentLookupQuotationDto>> LookupQuotationsAsync(
            CancellationToken cancellationToken = default);
    }
}
