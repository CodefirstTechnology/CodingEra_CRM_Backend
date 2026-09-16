using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("performance_reviews")]
public class PerformanceReview : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("review_period")]
    [MaxLength(32)]
    public string ReviewPeriod { get; set; } = string.Empty;

    [Column("key_achievements")]
    [MaxLength(1024)]
    public string KeyAchievements { get; set; } = string.Empty;

    [Column("manager_rating")]
    public decimal ManagerRating { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Pending";

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
