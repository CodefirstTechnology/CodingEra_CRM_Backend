using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("employee_documents")]
public class EmployeeDocument : ITenantEntity
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

    [Column("document_category_id")]
    public int DocumentCategoryId { get; set; }

    [ForeignKey(nameof(DocumentCategoryId))]
    public DocumentCategory? DocumentCategory { get; set; }

    [Column("document_name")]
    [MaxLength(256)]
    public string DocumentName { get; set; } = string.Empty;

    [Column("file_path")]
    [MaxLength(512)]
    public string FilePath { get; set; } = string.Empty;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
