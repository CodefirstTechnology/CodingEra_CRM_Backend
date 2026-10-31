using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IPerformanceRedisCacheManager
    {
        Task UpdateLeaderboardScoreAsync(
            string tenantId,
            int financialYear,
            string metric,
            int salesPersonUserId,
            double score,
            CancellationToken ct = default);

        Task<List<(int SalesPersonUserId, double Score)>> GetTopRanksAsync(
            string tenantId,
            int financialYear,
            string metric,
            int count = 50,
            CancellationToken ct = default);

        Task<long?> GetUserRankAsync(
            string tenantId,
            int financialYear,
            string metric,
            int salesPersonUserId,
            CancellationToken ct = default);

        Task InvalidateLeaderboardAsync(
            string tenantId,
            int financialYear,
            CancellationToken ct = default);
    }
}

