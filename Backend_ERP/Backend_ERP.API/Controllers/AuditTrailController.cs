using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/audit-trail")]
    [ApiController]
    public class AuditTrailController : ControllerBase
    {
        private readonly IProcurementAuditTrailService _auditTrailService;

        public AuditTrailController(IProcurementAuditTrailService auditTrailService)
        {
            _auditTrailService = auditTrailService;
        }

        [HttpGet]
        public async Task<ActionResult<List<AuditTrailEntryDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? module,
            [FromQuery] string? action,
            [FromQuery] string? user,
            [FromQuery] int? entityId,
            [FromQuery] string? entityNumber,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var list = await _auditTrailService.GetAuditEntriesAsync(new AuditTrailFilterQueryDto
            {
                Search = search,
                Module = module,
                Action = action,
                User = user,
                EntityId = entityId,
                EntityNumber = entityNumber,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken);

            return Ok(list);
        }

        [HttpGet("by-entity")]
        public async Task<ActionResult<List<AuditTrailEntryDto>>> GetByEntity(
            [FromQuery] string module,
            [FromQuery] int entityId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var list = await _auditTrailService.GetByEntityAsync(module, entityId, cancellationToken);
            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<AuditTrailEntryDto>> Record(
            [FromBody] AuditTrailRecordInputDto input,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            var user = ResolveUser(userId);
            var entry = await _auditTrailService.RecordAsync(input, user, cancellationToken);
            return CreatedAtAction(nameof(GetAll), new { id = entry.Id, userId }, entry);
        }

        private static string ResolveUser(int? userId) => userId is > 0 ? userId.Value.ToString() : "system";
    }
}
