using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IProcurementAuditTrailService
    {
        Task<List<AuditTrailEntryDto>> GetAuditEntriesAsync(AuditTrailFilterQueryDto query, CancellationToken cancellationToken = default);
        Task<List<AuditTrailEntryDto>> GetByEntityAsync(string module, int entityId, CancellationToken cancellationToken = default);
        Task<AuditTrailEntryDto> RecordAsync(AuditTrailRecordInputDto input, string currentUser, CancellationToken cancellationToken = default);
    }
}
