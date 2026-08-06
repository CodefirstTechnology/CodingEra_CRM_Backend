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

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
