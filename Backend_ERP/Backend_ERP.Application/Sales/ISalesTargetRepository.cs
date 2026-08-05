using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public interface ISalesTargetRepository
    {
        Task<IReadOnlyList<SalesTarget>> GetAllAsync(
            SalesTargetListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<SalesTarget?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default);

        Task<SalesTarget> CreateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default);

        Task<SalesTarget?> UpdateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default);

        Task<SalesTarget> DuplicateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default);

        Task<SalesTarget?> ActivateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default);

        Task<SalesTarget?> DeactivateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default);

        Task<SalesTarget?> UpdateProgressAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesTarget>> GetDashboardAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesTarget>> GetReportsAsync(
            SalesTargetListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<bool> TargetNumberExistsAsync(
            string targetNumber,
            int? excludeId,
            CancellationToken cancellationToken = default);

        Task<bool> HasOverlappingActiveTargetAsync(
            int? salesPersonUserId,
            string targetCategory,
            DateOnly startDate,
            DateOnly endDate,
            int? excludeId,
            CancellationToken cancellationToken = default);

        Task<SalesTarget?> FindPreviousTargetAsync(
            int? salesPersonUserId,
            int? financialYear,
            string? targetCategory,
            CancellationToken cancellationToken = default);

        IQueryable<SalesTarget> Query();
    }
}
