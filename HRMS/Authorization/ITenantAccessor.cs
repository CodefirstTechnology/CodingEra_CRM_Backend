namespace HRMS.Authorization;

public interface ITenantAccessor
{
    int? TenantId { get; }
    string? TenantCode { get; }
    bool HasTenant { get; }
    bool IsSuperAdmin { get; }
    void SetExplicitTenantId(int? tenantId);
}
