using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UserProfileResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? TenantCode { get; set; }
    public string? TenantPlan { get; set; }
    public int? EmployeeId { get; set; }
    public string? Status { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public string[] Permissions { get; set; } = Array.Empty<string>();
}

public class LoginActivityDto
{
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
