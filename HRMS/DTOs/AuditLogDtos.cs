namespace HRMS.DTOs;

public class AuditLogResponseDto
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
    public string? UserRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogQueryDto
{
    public int? TenantId { get; set; }
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
