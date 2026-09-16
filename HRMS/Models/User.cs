using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int? TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("full_name")]
    [MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    [Column("email")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("role_id")]
    public int RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    [Column("employee_id")]
    public int? EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("last_login_ip")]
    [MaxLength(64)]
    public string? LastLoginIp { get; set; }

    [Column("password_reset_required")]
    public bool PasswordResetRequired { get; set; } = false;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
