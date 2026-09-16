using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class TenantUpsertDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Domain { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? ContactPhone { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [MaxLength(32)]
    public string Plan { get; set; } = "Enterprise";

    public int MaxEmployees { get; set; } = 500;

    public DateTime? SubscriptionExpiresAt { get; set; }
}

public class TenantResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string Status { get; set; } = "Active";
    public string Plan { get; set; } = "Enterprise";
    public int MaxEmployees { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalBranches { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalUsers { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TenantStatusUpdateDto
{
    [Required]
    public string Status { get; set; } = "Active"; // Active, Suspended, Deactivated
}

public class TenantPlanUpdateDto
{
    [Required]
    public string Plan { get; set; } = "Enterprise";
    public int? MaxEmployees { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}

public class TenantAdminCreateDto
{
    [Required]
    [MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}
