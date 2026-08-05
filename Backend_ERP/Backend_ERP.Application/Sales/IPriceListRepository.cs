using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public interface IPriceListRepository
    {
        Task<IReadOnlyList<PriceList>> GetAllAsync(
            PriceListListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<PriceList?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default);

        Task<PriceList> CreateAsync(PriceList entity, CancellationToken cancellationToken = default);

        Task<PriceList?> UpdateAsync(PriceList entity, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(PriceList entity, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriceList>> GetActiveAsync(
            DateOnly? asOfDate,
            CancellationToken cancellationToken = default);

        Task<bool> PriceListNumberExistsAsync(
            string priceListNumber,
            int? excludeId,
            CancellationToken cancellationToken = default);

        Task<bool> HasOverlappingActiveAsync(
            string priceListName,
            string customerCategory,
            string currency,
            DateOnly effectiveFrom,
            DateOnly? effectiveTo,
            int? excludeId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriceListHistory>> GetHistoryAsync(
            int priceListId,
            CancellationToken cancellationToken = default);

        IQueryable<PriceList> Query();
    }
}
