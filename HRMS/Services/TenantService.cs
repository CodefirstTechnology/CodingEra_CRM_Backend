using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public interface ITenantService
{
    Task<IReadOnlyList<TenantResponseDto>> GetAllTenantsAsync(CancellationToken cancellationToken = default);
    Task<TenantResponseDto?> GetTenantByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(TenantResponseDto? Tenant, string? Error)> CreateTenantAsync(TenantUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(TenantResponseDto? Tenant, string? Error)> UpdateTenantAsync(int id, TenantUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(TenantResponseDto? Tenant, string? Error)> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default);
    Task<(TenantResponseDto? Tenant, string? Error)> UpdatePlanAsync(int id, TenantPlanUpdateDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserResponseDto>> GetTenantAdminsAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<(UserResponseDto? User, string? Error)> CreateTenantAdminAsync(int tenantId, TenantAdminCreateDto dto, CancellationToken cancellationToken = default);
}

public sealed class TenantService : ITenantService
{
    private readonly HRMSDbContext _context;
    private readonly IAuditService _auditService;

    public TenantService(HRMSDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<TenantResponseDto>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var tenantIds = tenants.Select(t => t.Id).ToList();

        // Get aggregate counts across tenants
        var empCounts = await _context.Employees
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => tenantIds.Contains(e.TenantId))
            .GroupBy(e => e.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var branchCounts = await _context.Branches
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(b => tenantIds.Contains(b.TenantId))
            .GroupBy(b => b.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var deptCounts = await _context.Departments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(d => tenantIds.Contains(d.TenantId))
            .GroupBy(d => d.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var userCounts = await _context.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.TenantId.HasValue && tenantIds.Contains(u.TenantId.Value))
            .GroupBy(u => u.TenantId!.Value)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        return tenants.Select(t => new TenantResponseDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            Domain = t.Domain,
            ContactEmail = t.ContactEmail,
            ContactPhone = t.ContactPhone,
            Status = t.Status,
            Plan = t.Plan,
            MaxEmployees = t.MaxEmployees,
            SubscriptionExpiresAt = t.SubscriptionExpiresAt,
            TotalEmployees = empCounts.GetValueOrDefault(t.Id, 0),
            TotalBranches = branchCounts.GetValueOrDefault(t.Id, 0),
            TotalDepartments = deptCounts.GetValueOrDefault(t.Id, 0),
            TotalUsers = userCounts.GetValueOrDefault(t.Id, 0),
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();
    }

    public async Task<TenantResponseDto?> GetTenantByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var t = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (t == null)
        {
            return null;
        }

        var totalEmployees = await _context.Employees
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == id, cancellationToken);

        var totalBranches = await _context.Branches
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == id, cancellationToken);

        var totalDepartments = await _context.Departments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == id, cancellationToken);

        var totalUsers = await _context.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == id, cancellationToken);

        return new TenantResponseDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            Domain = t.Domain,
            ContactEmail = t.ContactEmail,
            ContactPhone = t.ContactPhone,
            Status = t.Status,
            Plan = t.Plan,
            MaxEmployees = t.MaxEmployees,
            SubscriptionExpiresAt = t.SubscriptionExpiresAt,
            TotalEmployees = totalEmployees,
            TotalBranches = totalBranches,
            TotalDepartments = totalDepartments,
            TotalUsers = totalUsers,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }

    public async Task<(TenantResponseDto? Tenant, string? Error)> CreateTenantAsync(TenantUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var code = dto.Code.Trim().ToLowerInvariant();
        if (await _context.Tenants.AnyAsync(x => x.Code.ToLower() == code, cancellationToken))
        {
            return (null, "A tenant with this unique code already exists.");
        }

        var tenant = new Tenant
        {
            Name = dto.Name.Trim(),
            Code = code,
            Domain = dto.Domain?.Trim(),
            ContactEmail = dto.ContactEmail.Trim().ToLowerInvariant(),
            ContactPhone = dto.ContactPhone?.Trim(),
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
            Plan = string.IsNullOrWhiteSpace(dto.Plan) ? "Enterprise" : dto.Plan,
            MaxEmployees = dto.MaxEmployees > 0 ? dto.MaxEmployees : 500,
            SubscriptionExpiresAt = dto.SubscriptionExpiresAt ?? DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        // Auto-provision default branch, departments, and leave types for new tenant
        var defaultBranch = new Branch
        {
            TenantId = tenant.Id,
            Name = $"{tenant.Name} - Main Office",
            Code = "HQ",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Branches.Add(defaultBranch);
        await _context.SaveChangesAsync(cancellationToken);

        var defaultDepts = new[]
        {
            new Department { TenantId = tenant.Id, BranchId = defaultBranch.Id, Name = "Engineering", Code = "ENG", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Department { TenantId = tenant.Id, BranchId = defaultBranch.Id, Name = "Human Resources", Code = "HR", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Department { TenantId = tenant.Id, BranchId = defaultBranch.Id, Name = "Finance & Operations", Code = "FIN", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _context.Departments.AddRange(defaultDepts);

        var defaultDesigs = new[]
        {
            new Designation { TenantId = tenant.Id, Name = "Senior Software Engineer", Code = "SR_DEV", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Designation { TenantId = tenant.Id, Name = "HR Manager", Code = "HR_MGR", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Designation { TenantId = tenant.Id, Name = "Operations Lead", Code = "OPS_LEAD", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _context.Designations.AddRange(defaultDesigs);

        var defaultLeaveTypes = new[]
        {
            new LeaveType { TenantId = tenant.Id, Name = "Casual Leave", Code = "CL", DefaultAllocatedDays = 12, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new LeaveType { TenantId = tenant.Id, Name = "Sick Leave", Code = "SL", DefaultAllocatedDays = 10, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new LeaveType { TenantId = tenant.Id, Name = "Earned Leave", Code = "EL", DefaultAllocatedDays = 15, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _context.LeaveTypes.AddRange(defaultLeaveTypes);

        var defaultDocCats = new[]
        {
            new DocumentCategory { TenantId = tenant.Id, Name = "Identity (Aadhaar / Passport)", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DocumentCategory { TenantId = tenant.Id, Name = "Tax / PAN Card", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DocumentCategory { TenantId = tenant.Id, Name = "Educational Certificates", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _context.DocumentCategories.AddRange(defaultDocCats);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("CREATE_TENANT", "Tenant", tenant.Id.ToString(), new { tenant.Name, tenant.Code, tenant.Plan }, tenant.Id, cancellationToken);

        var result = await GetTenantByIdAsync(tenant.Id, cancellationToken);
        return (result, null);
    }

    public async Task<(TenantResponseDto? Tenant, string? Error)> UpdateTenantAsync(int id, TenantUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tenant == null)
        {
            return (null, "Tenant not found.");
        }

        var code = dto.Code.Trim().ToLowerInvariant();
        if (await _context.Tenants.AnyAsync(x => x.Id != id && x.Code.ToLower() == code, cancellationToken))
        {
            return (null, "A tenant with this unique code already exists.");
        }

        tenant.Name = dto.Name.Trim();
        tenant.Code = code;
        tenant.Domain = dto.Domain?.Trim();
        tenant.ContactEmail = dto.ContactEmail.Trim().ToLowerInvariant();
        tenant.ContactPhone = dto.ContactPhone?.Trim();
        tenant.Status = string.IsNullOrWhiteSpace(dto.Status) ? tenant.Status : dto.Status;
        tenant.Plan = string.IsNullOrWhiteSpace(dto.Plan) ? tenant.Plan : dto.Plan;
        if (dto.MaxEmployees > 0) tenant.MaxEmployees = dto.MaxEmployees;
        if (dto.SubscriptionExpiresAt.HasValue) tenant.SubscriptionExpiresAt = dto.SubscriptionExpiresAt.Value;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UPDATE_TENANT", "Tenant", tenant.Id.ToString(), new { tenant.Name, tenant.Code, tenant.Status, tenant.Plan }, tenant.Id, cancellationToken);

        var result = await GetTenantByIdAsync(tenant.Id, cancellationToken);
        return (result, null);
    }

    public async Task<(TenantResponseDto? Tenant, string? Error)> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tenant == null)
        {
            return (null, "Tenant not found.");
        }

        tenant.Status = status;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UPDATE_TENANT_STATUS", "Tenant", tenant.Id.ToString(), new { Status = status }, tenant.Id, cancellationToken);

        var result = await GetTenantByIdAsync(tenant.Id, cancellationToken);
        return (result, null);
    }

    public async Task<(TenantResponseDto? Tenant, string? Error)> UpdatePlanAsync(int id, TenantPlanUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tenant == null)
        {
            return (null, "Tenant not found.");
        }

        tenant.Plan = dto.Plan;
        if (dto.MaxEmployees.HasValue && dto.MaxEmployees.Value > 0)
        {
            tenant.MaxEmployees = dto.MaxEmployees.Value;
        }
        if (dto.SubscriptionExpiresAt.HasValue)
        {
            tenant.SubscriptionExpiresAt = dto.SubscriptionExpiresAt.Value;
        }
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UPDATE_TENANT_PLAN", "Tenant", tenant.Id.ToString(), new { dto.Plan, dto.MaxEmployees }, tenant.Id, cancellationToken);

        var result = await GetTenantByIdAsync(tenant.Id, cancellationToken);
        return (result, null);
    }

    public async Task<IReadOnlyList<UserResponseDto>> GetTenantAdminsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var hrAdminRoleId = RoleSeed.HrAdmin.Id;
        return await _context.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.Role)
            .Include(x => x.Tenant)
            .Where(x => x.TenantId == tenantId && x.RoleId == hrAdminRoleId)
            .OrderBy(x => x.FullName)
            .Select(x => new UserResponseDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : null,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role.Code,
                EmployeeId = x.EmployeeId,
                Status = x.Status,
                IsActive = x.IsActive,
                LastLoginAt = x.LastLoginAt,
                LastLoginIp = x.LastLoginIp,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(UserResponseDto? User, string? Error)> CreateTenantAdminAsync(int tenantId, TenantAdminCreateDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (tenant == null)
        {
            return (null, "Tenant not found.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Email.ToLower() == email, cancellationToken))
        {
            return (null, "A user with this email already exists.");
        }

        var hrAdminRoleId = RoleSeed.HrAdmin.Id;
        var user = new User
        {
            TenantId = tenantId,
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = hrAdminRoleId,
            Status = "Active",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("CREATE_TENANT_ADMIN", "User", user.Id.ToString(), new { user.Email, user.FullName, TenantId = tenantId }, tenantId, cancellationToken);

        return (new UserResponseDto
        {
            Id = user.Id,
            TenantId = user.TenantId,
            TenantName = tenant.Name,
            FullName = user.FullName,
            Email = user.Email,
            Role = nameof(UserRole.HR_ADMIN),
            EmployeeId = null,
            Status = user.Status,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        }, null);
    }
}
