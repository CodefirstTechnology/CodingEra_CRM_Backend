using HRMS.Authorization;
using HRMS.DTOs;
using HRMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Route("api/tenants")]
[ApiController]
[Authorize(Roles = "SUPER_ADMIN")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserAccessor _currentUser;

    public TenantsController(ITenantService tenantService, ICurrentUserAccessor currentUser)
    {
        _tenantService = tenantService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequirePermission(HrmsPermissions.TenantsView)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var tenants = await _tenantService.GetAllTenantsAsync(cancellationToken);
        return Ok(tenants);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(HrmsPermissions.TenantsView)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetTenantByIdAsync(id, cancellationToken);
        return tenant == null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    [RequirePermission(HrmsPermissions.TenantsManage)]
    public async Task<IActionResult> Create([FromBody] TenantUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (tenant, error) = await _tenantService.CreateTenantAsync(dto, cancellationToken);
        if (error != null)
        {
            return Conflict(new { message = error });
        }

        return Ok(tenant);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(HrmsPermissions.TenantsManage)]
    public async Task<IActionResult> Update(int id, [FromBody] TenantUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (tenant, error) = await _tenantService.UpdateTenantAsync(id, dto, cancellationToken);
        if (error != null)
        {
            return Conflict(new { message = error });
        }

        return tenant == null ? NotFound() : Ok(tenant);
    }

    [HttpPatch("{id:int}/status")]
    [RequirePermission(HrmsPermissions.TenantsManage)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] TenantStatusUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (tenant, error) = await _tenantService.UpdateStatusAsync(id, dto.Status, cancellationToken);
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return tenant == null ? NotFound() : Ok(tenant);
    }

    [HttpPatch("{id:int}/plan")]
    [RequirePermission(HrmsPermissions.TenantsManage)]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] TenantPlanUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (tenant, error) = await _tenantService.UpdatePlanAsync(id, dto, cancellationToken);
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return tenant == null ? NotFound() : Ok(tenant);
    }

    [HttpGet("{id:int}/admins")]
    [RequirePermission(HrmsPermissions.TenantAdminsManage)]
    public async Task<IActionResult> GetAdmins(int id, CancellationToken cancellationToken)
    {
        var admins = await _tenantService.GetTenantAdminsAsync(id, cancellationToken);
        return Ok(admins);
    }

    [HttpPost("{id:int}/admins")]
    [RequirePermission(HrmsPermissions.TenantAdminsManage)]
    public async Task<IActionResult> CreateAdmin(int id, [FromBody] TenantAdminCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (user, error) = await _tenantService.CreateTenantAdminAsync(id, dto, cancellationToken);
        if (error != null)
        {
            return Conflict(new { message = error });
        }

        return Ok(user);
    }
}
