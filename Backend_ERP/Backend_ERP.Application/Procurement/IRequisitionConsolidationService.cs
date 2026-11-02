using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IRequisitionConsolidationService
    {
        Task<List<ConsolidationCandidateLineDto>> GetConsolidationPoolAsync(int tenantId = 1, CancellationToken cancellationToken = default);

        Task<RFQDto> ConsolidateLinesToRFQAsync(ConsolidateLinesRequestDto request, string actingUser, int tenantId = 1, CancellationToken cancellationToken = default);
    }
}

