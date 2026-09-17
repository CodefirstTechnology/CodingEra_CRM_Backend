namespace ERP.Application.Sales
{
    public interface IPerformanceRecalculationService
    {
        Task RecalculateRepPerformanceAsync(int salesPersonUserId, int financialYear, CancellationToken ct = default);
        Task GenerateMonthlySnapshotsAsync(int financialYear, string periodKey, CancellationToken ct = default);
    }
}
