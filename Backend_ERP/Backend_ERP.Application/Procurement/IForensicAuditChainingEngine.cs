using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public interface IForensicAuditChainingEngine
    {
        Task<ProcurementAuditLog> AppendAuditLogAsync(string entityName, string entityId, string actionType, string changedByUser, string? userIp, object? oldValues, object? newValues, CancellationToken ct = default);
        Task<AuditChainVerificationResultDto> VerifyChainIntegrityAsync(long startBlock = 1, long? endBlock = null, CancellationToken ct = default);
    }
}

