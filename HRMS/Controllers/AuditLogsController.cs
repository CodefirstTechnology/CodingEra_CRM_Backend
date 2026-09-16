using HRMS.Authorization;
using HRMS.DTOs;
using HRMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Route("api/audit-logs")]
[ApiController]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ICurrentUserAccessor _currentUser;

    public AuditLogsController(IAuditService auditService, ICurrentUserAccessor currentUser)
    {
        _auditService = auditService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? tenantId,
        [FromQuery] int? userId,
        [FromQuery] string? action,
        [FromQuery] string? entityName,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin)
        {
            return Forbid();
        }

        var logs = await _auditService.GetLogsAsync(new AuditLogQueryDto
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityName = entityName,
            FromDate = fromDate,
            ToDate = toDate,
            Search = search,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(logs);
    }
}
