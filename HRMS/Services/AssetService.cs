using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public interface IAssetService
{
    Task<IReadOnlyList<AssetResponseDto>> GetAssetsAsync(int? branchId, string? category, string? status, int? employeeId, CancellationToken cancellationToken = default);
    Task<AssetResponseDto?> GetAssetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(AssetResponseDto? Asset, string? Error)> CreateAssetAsync(AssetUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(AssetResponseDto? Asset, string? Error)> UpdateAssetAsync(int id, AssetUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(AssetResponseDto? Asset, string? Error)> AssignAssetAsync(int id, AssetAssignDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAssetAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class AssetService : IAssetService
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IAuditService _auditService;

    public AssetService(
        HRMSDbContext context,
        ICurrentUserAccessor currentUser,
        ITenantAccessor tenantAccessor,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantAccessor = tenantAccessor;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<AssetResponseDto>> GetAssetsAsync(
        int? branchId,
        string? category,
        string? status,
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CompanyAssets
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.AssignedToEmployee)
            .AsQueryable();

        // If employee role, restrict to their own assigned assets
        if (_currentUser.IsEmployee)
        {
            if (!_currentUser.EmployeeId.HasValue)
            {
                return Array.Empty<AssetResponseDto>();
            }

            query = query.Where(x => x.AssignedToEmployeeId == _currentUser.EmployeeId.Value);
        }
        else if (employeeId.HasValue)
        {
            query = query.Where(x => x.AssignedToEmployeeId == employeeId.Value);
        }

        if (branchId.HasValue)
        {
            query = query.Where(x => x.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AssetResponseDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : null,
                AssetCode = x.AssetCode,
                Name = x.Name,
                Category = x.Category,
                SerialNumber = x.SerialNumber,
                ModelNumber = x.ModelNumber,
                Status = x.Status,
                PurchaseDate = x.PurchaseDate,
                AssignedToEmployeeId = x.AssignedToEmployeeId,
                AssignedToEmployeeName = x.AssignedToEmployee != null ? x.AssignedToEmployee.FullName : null,
                AssignedToEmployeeCode = x.AssignedToEmployee != null ? x.AssignedToEmployee.EmployeeCode : null,
                AssignedAt = x.AssignedAt,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AssetResponseDto?> GetAssetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyAssets
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.AssignedToEmployee)
            .Where(x => x.Id == id)
            .Select(x => new AssetResponseDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : null,
                AssetCode = x.AssetCode,
                Name = x.Name,
                Category = x.Category,
                SerialNumber = x.SerialNumber,
                ModelNumber = x.ModelNumber,
                Status = x.Status,
                PurchaseDate = x.PurchaseDate,
                AssignedToEmployeeId = x.AssignedToEmployeeId,
                AssignedToEmployeeName = x.AssignedToEmployee != null ? x.AssignedToEmployee.FullName : null,
                AssignedToEmployeeCode = x.AssignedToEmployee != null ? x.AssignedToEmployee.EmployeeCode : null,
                AssignedAt = x.AssignedAt,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(AssetResponseDto? Asset, string? Error)> CreateAssetAsync(AssetUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.TenantId ?? 1;

        var assetCode = dto.AssetCode.Trim().ToUpperInvariant();
        if (await _context.CompanyAssets.AnyAsync(x => x.AssetCode == assetCode, cancellationToken))
        {
            return (null, "An asset with this asset code already exists.");
        }

        if (dto.AssignedToEmployeeId.HasValue &&
            !await _context.Employees.AnyAsync(x => x.Id == dto.AssignedToEmployeeId.Value, cancellationToken))
        {
            return (null, "Assigned employee was not found.");
        }

        var asset = new CompanyAsset
        {
            TenantId = tenantId,
            BranchId = dto.BranchId,
            AssetCode = assetCode,
            Name = dto.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "Laptop" : dto.Category.Trim(),
            SerialNumber = dto.SerialNumber?.Trim(),
            ModelNumber = dto.ModelNumber?.Trim(),
            Status = dto.AssignedToEmployeeId.HasValue ? "Assigned" : (string.IsNullOrWhiteSpace(dto.Status) ? "Available" : dto.Status),
            PurchaseDate = dto.PurchaseDate,
            AssignedToEmployeeId = dto.AssignedToEmployeeId,
            AssignedAt = dto.AssignedToEmployeeId.HasValue ? DateTime.UtcNow : null,
            Notes = dto.Notes?.Trim(),
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CompanyAssets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("CREATE_ASSET", "CompanyAsset", asset.Id.ToString(), new { asset.AssetCode, asset.Name, asset.Category }, asset.TenantId, cancellationToken);

        var result = await GetAssetByIdAsync(asset.Id, cancellationToken);
        return (result, null);
    }

    public async Task<(AssetResponseDto? Asset, string? Error)> UpdateAssetAsync(int id, AssetUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var asset = await _context.CompanyAssets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (asset == null)
        {
            return (null, "Asset not found.");
        }

        var assetCode = dto.AssetCode.Trim().ToUpperInvariant();
        if (await _context.CompanyAssets.AnyAsync(x => x.Id != id && x.AssetCode == assetCode, cancellationToken))
        {
            return (null, "An asset with this asset code already exists.");
        }

        asset.BranchId = dto.BranchId;
        asset.AssetCode = assetCode;
        asset.Name = dto.Name.Trim();
        asset.Category = string.IsNullOrWhiteSpace(dto.Category) ? asset.Category : dto.Category.Trim();
        asset.SerialNumber = dto.SerialNumber?.Trim();
        asset.ModelNumber = dto.ModelNumber?.Trim();
        asset.PurchaseDate = dto.PurchaseDate;
        asset.Notes = dto.Notes?.Trim();
        asset.UpdatedBy = _currentUser.UserId;
        asset.UpdatedAt = DateTime.UtcNow;

        if (dto.AssignedToEmployeeId != asset.AssignedToEmployeeId)
        {
            asset.AssignedToEmployeeId = dto.AssignedToEmployeeId;
            asset.AssignedAt = dto.AssignedToEmployeeId.HasValue ? DateTime.UtcNow : null;
            asset.Status = dto.AssignedToEmployeeId.HasValue ? "Assigned" : "Available";
        }
        else if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            asset.Status = dto.Status;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UPDATE_ASSET", "CompanyAsset", asset.Id.ToString(), new { asset.AssetCode, asset.Status }, asset.TenantId, cancellationToken);

        var result = await GetAssetByIdAsync(asset.Id, cancellationToken);
        return (result, null);
    }

    public async Task<(AssetResponseDto? Asset, string? Error)> AssignAssetAsync(int id, AssetAssignDto dto, CancellationToken cancellationToken = default)
    {
        var asset = await _context.CompanyAssets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (asset == null)
        {
            return (null, "Asset not found.");
        }

        if (dto.EmployeeId.HasValue)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(x => x.Id == dto.EmployeeId.Value, cancellationToken);
            if (employee == null)
            {
                return (null, "Employee not found.");
            }

            asset.AssignedToEmployeeId = dto.EmployeeId.Value;
            asset.AssignedAt = DateTime.UtcNow;
            asset.Status = "Assigned";
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                asset.Notes = dto.Notes.Trim();
            }
        }
        else
        {
            asset.AssignedToEmployeeId = null;
            asset.AssignedAt = null;
            asset.Status = "Available";
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                asset.Notes = dto.Notes.Trim();
            }
        }

        asset.UpdatedBy = _currentUser.UserId;
        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(dto.EmployeeId.HasValue ? "ASSIGN_ASSET" : "UNASSIGN_ASSET", "CompanyAsset", asset.Id.ToString(), new { EmployeeId = dto.EmployeeId }, asset.TenantId, cancellationToken);

        var result = await GetAssetByIdAsync(asset.Id, cancellationToken);
        return (result, null);
    }

    public async Task<bool> DeleteAssetAsync(int id, CancellationToken cancellationToken = default)
    {
        var asset = await _context.CompanyAssets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (asset == null)
        {
            return false;
        }

        _context.CompanyAssets.Remove(asset);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("DELETE_ASSET", "CompanyAsset", id.ToString(), new { asset.AssetCode }, asset.TenantId, cancellationToken);

        return true;
    }
}
