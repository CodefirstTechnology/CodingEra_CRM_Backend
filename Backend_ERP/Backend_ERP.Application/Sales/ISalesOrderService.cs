using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface ISalesOrderService
    {
        Task<IReadOnlyList<SalesOrderListItemDto>> GetAllAsync(
            SalesOrderListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<SalesOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<SalesOrderDto> CreateAsync(
            SalesOrderCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesOrderDto?> UpdateAsync(
            int id,
            SalesOrderUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesOrderDto?> UpdateStatusAsync(
            int id,
            SalesOrderStatusUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesOrderDto?> CancelAsync(
            int id,
            SalesOrderCancelRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesOrderDto> ConvertQuotationAsync(
            int quotationApprovalId,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesOrderStatusHistoryDto>?> GetStatusHistoryAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<SalesOrderPdfResultDto?> GeneratePdfAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<SalesOrderEmailResultDto?> SendEmailAsync(
            int id,
            SalesOrderEmailRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesOrderDto?> SetCreditHoldAsync(
            int id,
            string reason,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesOrderDto?> OverrideCreditHoldAsync(
            int id,
            string justification,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesOrderDto?> SetProductionReleasedAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesOrderDto?> SetDispatchReleasedAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetPermissionsAsync();
    }
}
