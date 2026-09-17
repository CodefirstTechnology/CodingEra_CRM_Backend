using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("branches")]
public class Branch : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("name")]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    [MaxLength(32)]
    public string? Code { get; set; }

    [Column("branch_type")]
    [MaxLength(64)]
    public string BranchType { get; set; } = "Office"; // Head Office, Regional Office, Branch, Tech Hub

    [Column("address")]
    [MaxLength(256)]
    public string? Address { get; set; }

    [Column("city")]
    [MaxLength(128)]
    public string? City { get; set; }

    [Column("state")]
    [MaxLength(128)]
    public string? State { get; set; }

    [Column("country")]
    [MaxLength(128)]
    public string? Country { get; set; }

    [Column("pin_code")]
    [MaxLength(32)]
    public string? PinCode { get; set; }

    [Column("contact_person")]
    [MaxLength(128)]
    public string? ContactPerson { get; set; }

    [Column("contact_email")]
    [MaxLength(256)]
    public string? ContactEmail { get; set; }

    [Column("contact_phone")]
    [MaxLength(64)]
    public string? ContactPhone { get; set; }

    [Column("working_hours")]
    [MaxLength(64)]
    public string? WorkingHours { get; set; } = "09:00 - 18:00";

    [Column("timezone")]
    [MaxLength(64)]
    public string? Timezone { get; set; } = "Asia/Kolkata";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
