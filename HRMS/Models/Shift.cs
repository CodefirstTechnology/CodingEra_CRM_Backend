using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("shifts")]
public class Shift : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("shift_code")]
    [MaxLength(32)]
    public string ShiftCode { get; set; } = string.Empty;

    [Column("shift_name")]
    [MaxLength(128)]
    public string ShiftName { get; set; } = string.Empty;

    [Column("start_time")]
    [MaxLength(16)]
    public string StartTime { get; set; } = "09:00";

    [Column("end_time")]
    [MaxLength(16)]
    public string EndTime { get; set; } = "18:00";

    [Column("grace_period_minutes")]
    public int GracePeriodMinutes { get; set; } = 15;

    [Column("break_duration_minutes")]
    public int BreakDurationMinutes { get; set; } = 60;

    [Column("working_hours")]
    public decimal WorkingHours { get; set; } = 8.0m;

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
