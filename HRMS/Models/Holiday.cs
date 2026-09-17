using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("holidays")]
public class Holiday : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("holiday_name")]
    [MaxLength(128)]
    public string HolidayName { get; set; } = string.Empty;

    [Column("holiday_date")]
    public DateOnly HolidayDate { get; set; }

    [Column("holiday_type")]
    [MaxLength(64)]
    public string HolidayType { get; set; } = "Public Holiday"; // Company Holiday, Public Holiday, Optional Holiday

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Column("is_optional")]
    public bool IsOptional { get; set; } = false;

    [Column("description")]
    [MaxLength(256)]
    public string? Description { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
