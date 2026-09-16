using System.Text.Json;
using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? entityId = null, object? details = null, int? explicitTenantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogResponseDto>> GetLogsAsync(AuditLogQueryDto query, CancellationToken cancellationToken = default);
}

public sealed class AuditService : IAuditService
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantAccessor _tenantAccessor;

    public AuditService(HRMSDbContext context, ICurrentUserAccessor currentUser, ITenantAccessor tenantAccessor)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantAccessor = tenantAccessor;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        object? details = null,
        int? explicitTenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = explicitTenantId ?? _tenantAccessor.TenantId;
            var detailsString = details switch
            {
                null => null,
                string s => s,
                _ => JsonSerializer.Serialize(details)
            };

            var log = new AuditLog
            {
                TenantId = tenantId,
                UserId = _currentUser.UserId,
                UserEmail = _currentUser.Email,
                UserName = _currentUser.FullName,
                UserRole = _currentUser.Role?.ToString(),
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = detailsString,
                IpAddress = _currentUser.IpAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Do not fail main request if audit log recording hits a non-critical error
        }
    }

    public async Task<IReadOnlyList<AuditLogResponseDto>> GetLogsAsync(AuditLogQueryDto query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.AuditLogs
            .AsNoTracking()
            .Include(x => x.Tenant)
            .AsQueryable();

        // If not SuperAdmin, restrict to current user's tenant
        if (!_currentUser.IsSuperAdmin)
        {
            if (!_tenantAccessor.HasTenant)
            {
                return Array.Empty<AuditLogResponseDto>();
            }

            var tenantId = _tenantAccessor.TenantId!.Value;
            dbQuery = dbQuery.Where(x => x.TenantId == tenantId);
        }
        else
        {
            // SuperAdmin can filter by tenant
            if (query.TenantId.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.TenantId == query.TenantId.Value);
            }
            else if (_tenantAccessor.HasTenant)
            {
                var tenantId = _tenantAccessor.TenantId!.Value;
                dbQuery = dbQuery.Where(x => x.TenantId == tenantId);
            }
        }

        if (query.UserId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            dbQuery = dbQuery.Where(x => x.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            dbQuery = dbQuery.Where(x => x.EntityName == query.EntityName);
        }

        if (query.FromDate.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAt <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            dbQuery = dbQuery.Where(x =>
                (x.Action != null && x.Action.ToLower().Contains(s)) ||
                (x.EntityName != null && x.EntityName.ToLower().Contains(s)) ||
                (x.UserEmail != null && x.UserEmail.ToLower().Contains(s)) ||
                (x.UserName != null && x.UserName.ToLower().Contains(s)) ||
                (x.Details != null && x.Details.ToLower().Contains(s)));
        }

        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var page = Math.Max(query.Page, 1);

        return await dbQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogResponseDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : null,
                UserId = x.UserId,
                UserEmail = x.UserEmail,
                UserName = x.UserName,
                UserRole = x.UserRole,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Details = x.Details,
                IpAddress = x.IpAddress,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
