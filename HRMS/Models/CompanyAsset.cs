using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("company_assets")]
public class CompanyAsset : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Column("asset_code")]
    [MaxLength(64)]
    public string AssetCode { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Column("category")]
    [MaxLength(64)]
    public string Category { get; set; } = "Laptop";

    [Column("serial_number")]
    [MaxLength(128)]
    public string? SerialNumber { get; set; }

    [Column("model_number")]
    [MaxLength(128)]
    public string? ModelNumber { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Available";

    [Column("purchase_date")]
    public DateOnly? PurchaseDate { get; set; }

    [Column("assigned_to_employee_id")]
    public int? AssignedToEmployeeId { get; set; }

    [ForeignKey(nameof(AssignedToEmployeeId))]
    public Employee? AssignedToEmployee { get; set; }

    [Column("assigned_at")]
    public DateTime? AssignedAt { get; set; }

    [Column("notes")]
    [MaxLength(512)]
    public string? Notes { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
