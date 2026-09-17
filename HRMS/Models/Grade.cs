using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("grades")]
public class Grade : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("grade_code")]
    [MaxLength(32)]
    public string GradeCode { get; set; } = string.Empty;

    [Column("grade_name")]
    [MaxLength(128)]
    public string GradeName { get; set; } = string.Empty;

    [Column("level")]
    public int Level { get; set; } = 1;

    [Column("description")]
    [MaxLength(256)]
    public string? Description { get; set; }

    [Column("min_salary")]
    public decimal? MinSalary { get; set; }

    [Column("max_salary")]
    public decimal? MaxSalary { get; set; }

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
