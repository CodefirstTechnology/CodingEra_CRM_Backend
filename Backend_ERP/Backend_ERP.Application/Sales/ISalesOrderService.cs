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

        Task<IReadOnlyList<SalesOrderStatusHistoryDto>?> GetStatusHistoryAsync(
            int id,
            CancellationToken cancellationToken = default);
    }
}
