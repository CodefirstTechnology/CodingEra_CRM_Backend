using HRMS.Authorization;
using HRMS.DTOs;
using HRMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Route("api/assets")]
[ApiController]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly ICurrentUserAccessor _currentUser;

    public AssetsController(IAssetService assetService, ICurrentUserAccessor currentUser)
    {
        _assetService = assetService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? branchId,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] int? employeeId,
        CancellationToken cancellationToken)
    {
        var assets = await _assetService.GetAssetsAsync(branchId, category, status, employeeId, cancellationToken);
        return Ok(assets);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var asset = await _assetService.GetAssetByIdAsync(id, cancellationToken);
        if (asset == null)
        {
            return NotFound();
        }

        if (_currentUser.IsEmployee && asset.AssignedToEmployeeId != _currentUser.EmployeeId)
        {
            return Forbid();
        }

        return Ok(asset);
    }

    [HttpPost]
    [RequirePermission(HrmsPermissions.AssetsManage)]
    public async Task<IActionResult> Create([FromBody] AssetUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (asset, error) = await _assetService.CreateAssetAsync(dto, cancellationToken);
        if (error != null)
        {
            return Conflict(new { message = error });
        }

        return Ok(asset);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(HrmsPermissions.AssetsManage)]
    public async Task<IActionResult> Update(int id, [FromBody] AssetUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (asset, error) = await _assetService.UpdateAssetAsync(id, dto, cancellationToken);
        if (error != null)
        {
            return Conflict(new { message = error });
        }

        return asset == null ? NotFound() : Ok(asset);
    }

    [HttpPatch("{id:int}/assign")]
    [RequirePermission(HrmsPermissions.AssetsManage)]
    public async Task<IActionResult> Assign(int id, [FromBody] AssetAssignDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (asset, error) = await _assetService.AssignAssetAsync(id, dto, cancellationToken);
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return asset == null ? NotFound() : Ok(asset);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.AssetsManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _assetService.DeleteAssetAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
