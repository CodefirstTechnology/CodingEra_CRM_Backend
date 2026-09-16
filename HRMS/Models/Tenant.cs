using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("tenants")]
public class Tenant
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("name")]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [Column("domain")]
    [MaxLength(128)]
    public string? Domain { get; set; }

    [Column("contact_email")]
    [MaxLength(256)]
    public string ContactEmail { get; set; } = string.Empty;

    [Column("contact_phone")]
    [MaxLength(32)]
    public string? ContactPhone { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [Column("plan")]
    [MaxLength(32)]
    public string Plan { get; set; } = "Enterprise";

    [Column("max_employees")]
    public int MaxEmployees { get; set; } = 500;

    [Column("subscription_expires_at")]
    public DateTime? SubscriptionExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
