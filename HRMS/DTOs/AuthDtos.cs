namespace HRMS.DTOs;

public sealed class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthUserDto
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
    public IReadOnlyCollection<string> Permissions { get; set; } = Array.Empty<string>();
}

public sealed class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AuthUserDto User { get; set; } = new();
}

public sealed class UserUpsertDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? TenantId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
}

public sealed class UserResponseDto
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public string? Status { get; set; } = "Active";
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
