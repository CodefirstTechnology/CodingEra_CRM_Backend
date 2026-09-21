using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("document_versions")]
public class DocumentVersion : ITenantEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("employee_document_id")]
    public int EmployeeDocumentId { get; set; }

    [ForeignKey(nameof(EmployeeDocumentId))]
    public EmployeeDocument? EmployeeDocument { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("document_type_id")]
    public int? DocumentTypeId { get; set; }

    [ForeignKey(nameof(DocumentTypeId))]
    public DocumentType? DocumentType { get; set; }

    [Column("version_number")]
    public int VersionNumber { get; set; } = 1;

    [Column("file_path")]
    [MaxLength(512)]
    public string FilePath { get; set; } = string.Empty;

    [Column("file_name")]
    [MaxLength(256)]
    public string FileName { get; set; } = string.Empty;

    [Column("file_size")]
    public long FileSize { get; set; }

    [Column("mime_type")]
    [MaxLength(128)]
    public string? MimeType { get; set; }

    [Column("document_number")]
    [MaxLength(128)]
    public string? DocumentNumber { get; set; }

    [Column("issue_date")]
    public DateOnly? IssueDate { get; set; }

    [Column("expiry_date")]
    public DateOnly? ExpiryDate { get; set; }

    [Column("status")]
    [MaxLength(64)]
    public string Status { get; set; } = "Uploaded";

    [Column("rejection_reason")]
    [MaxLength(512)]
    public string? RejectionReason { get; set; }

    [Column("uploaded_by_user_id")]
    public int? UploadedByUserId { get; set; }

    [Column("uploaded_by_name")]
    [MaxLength(128)]
    public string? UploadedByName { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Column("verified_by_user_id")]
    public int? VerifiedByUserId { get; set; }

    [Column("verified_by_name")]
    [MaxLength(128)]
    public string? VerifiedByName { get; set; }

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [Column("remarks")]
    [MaxLength(512)]
    public string? Remarks { get; set; }
}
