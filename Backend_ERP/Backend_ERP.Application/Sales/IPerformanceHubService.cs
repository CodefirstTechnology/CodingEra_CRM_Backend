using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IPerformanceHubService
    {
        Task<RepOverviewCockpitDto> GetRepOverviewAsync(
            int salesPersonUserId,
            int financialYear,
            CancellationToken ct = default);

        Task<LeaderboardPodiumDto> GetLeaderboardAsync(
            LeaderboardFilterDto filter,
            CancellationToken ct = default);

        Task<List<SalesCommissionLedgerDto>> GetRepCommissionLedgerAsync(
            int salesPersonUserId,
            int financialYear,
            CancellationToken ct = default);

        Task<bool> ApproveCommissionEntryAsync(
            CommissionApprovalRequestDto request,
            int approverUserId,
            CancellationToken ct = default);

        Task<bool> DisburseCommissionPayoutAsync(
            CommissionDisbursementRequestDto request,
            int disburserUserId,
            CancellationToken ct = default);

        Task<List<IncentiveSchemeDto>> GetIncentiveSchemesAsync(
            int financialYear,
            CancellationToken ct = default);

        Task<IncentiveSchemeDto> CreateIncentiveSchemeAsync(
            CreateIncentiveSchemeDto request,
            int creatorUserId,
            CancellationToken ct = default);

        Task<bool> ToggleSchemeActiveAsync(
            int schemeId,
            bool isActive,
            CancellationToken ct = default);
    }
}

