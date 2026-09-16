using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int? TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Column("user_email")]
    [MaxLength(256)]
    public string? UserEmail { get; set; }

    [Column("user_name")]
    [MaxLength(256)]
    public string? UserName { get; set; }

    [Column("user_role")]
    [MaxLength(64)]
    public string? UserRole { get; set; }

    [Column("action")]
    [MaxLength(128)]
    public string Action { get; set; } = string.Empty;

    [Column("entity_name")]
    [MaxLength(128)]
    public string EntityName { get; set; } = string.Empty;

    [Column("entity_id")]
    [MaxLength(64)]
    public string? EntityId { get; set; }

    [Column("details")]
    public string? Details { get; set; }

    [Column("ip_address")]
    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
