using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IPerformanceService
    {
        Task<PerformanceDashboardDto> GetDashboardAsync(PerformanceFilterDto filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceLeaderboardDto>> GetLeaderboardsAsync(PerformanceFilterDto filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceSalesPersonDto>> GetSalespersonsAsync(PerformanceFilterDto filter, CancellationToken cancellationToken = default);
        Task<PerformanceSalesPersonDto?> GetSalespersonAsync(int id, PerformanceFilterDto filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceTimelineDto>> GetTimelineAsync(int id, PerformanceFilterDto filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceTrendDto>> GetMonthlyAsync(int id, PerformanceFilterDto filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceHistoryDto>> GetPerformanceHistoryAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceHistoryDto>> GetTargetHistoryAsync(int id, CancellationToken cancellationToken = default);
        Task<PerformanceAnalyticsDto> GetAnalyticsAsync(PerformanceFilterDto filter, CancellationToken cancellationToken = default);
        Task<PerformanceReportDto> GetReportsAsync(PerformanceFilterDto filter, CancellationToken cancellationToken = default);
        Task<PerformanceExportDto> ExportAsync(PerformanceExportRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceExportDto>> GetExportHistoryAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceLookupDto>> LookupSalespersonsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceLookupDto>> LookupTeamsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceLookupDto>> LookupBranchesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PerformanceLookupDto>> LookupRegionalManagersAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetPermissionsAsync();
    }

    public static class PerformanceCalculator
    {
        public static decimal Percentage(decimal numerator, decimal denominator) =>
            denominator <= 0 ? 0m : Math.Round(numerator / denominator * 100m, 2, MidpointRounding.AwayFromZero);
    }
}
