using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class DocumentSummaryDto
{
    public int TotalDocuments { get; set; }
    public int VerifiedCount { get; set; }
    public int PendingVerificationCount { get; set; }
    public int RejectedCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ExpiredCount { get; set; }
    public int MissingDocumentsCount { get; set; }
    public decimal VerificationRate { get; set; }
    public List<CategoryCountDto> CategoryDistribution { get; set; } = new();
    public List<RecentDocumentActivityDto> RecentActivities { get; set; } = new();
}

public class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentDocumentActivityDto
{
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class DocumentTypeDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsMandatoryDuringOnboarding { get; set; }
    public bool RequiresVerification { get; set; } = true;
    public bool HasExpiry { get; set; }
    public int? DefaultValidityPeriodMonths { get; set; }
    public string AllowedFileTypes { get; set; } = "pdf,jpg,jpeg,png";
    public int MaximumFileSizeMb { get; set; } = 5;
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public int ActiveDocumentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DocumentTypeUpsertDto
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Category { get; set; } = "Other";

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsRequired { get; set; } = true;
    public bool IsMandatoryDuringOnboarding { get; set; }
    public bool RequiresVerification { get; set; } = true;
    public bool HasExpiry { get; set; }
    public int? DefaultValidityPeriodMonths { get; set; }

    [MaxLength(128)]
    public string AllowedFileTypes { get; set; } = "pdf,jpg,jpeg,png";

    [Range(1, 50)]
    public int MaximumFileSizeMb { get; set; } = 5;

    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    public bool IsActive { get; set; } = true;
}

public class DocumentRequirementDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int DocumentTypeId { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? EmploymentType { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? DesignationId { get; set; }
    public string? DesignationName { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? Gender { get; set; }
    public bool IsMandatory { get; set; } = true;
    public bool IsOnboardingMandatory { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DocumentRequirementUpsertDto
{
    [Required]
    public int DocumentTypeId { get; set; }

    [MaxLength(64)]
    public string? EmploymentType { get; set; }

    public int? DepartmentId { get; set; }
    public int? DesignationId { get; set; }
    public int? BranchId { get; set; }

    [MaxLength(32)]
    public string? Gender { get; set; }

    public bool IsMandatory { get; set; } = true;
    public bool IsOnboardingMandatory { get; set; } = true;
    public bool IsActive { get; set; } = true;

    [MaxLength(512)]
    public string? Remarks { get; set; }
}

public class EmployeeDocumentResponseDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int? DocumentTypeId { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? MaskedDocumentNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public string ExpiryStatus { get; set; } = "Valid"; // Valid, Expiring Soon, Expired, No Expiry
    public string FilePath { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? FileSizeFormatted { get; set; }
    public string? MimeType { get; set; }
    public int VersionNumber { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public string Status { get; set; } = "Pending Verification";
    public string? RejectionReason { get; set; }
    public int? VerifiedByUserId { get; set; }
    public string? VerifiedByName { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Remarks { get; set; }
    public bool IsArchived { get; set; }
    public DateTime UploadedAt { get; set; }
    public List<DocumentVersionDto> Versions { get; set; } = new();
}

public class DocumentVersionDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? VerifiedByName { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Remarks { get; set; }
}

public class DocumentUploadDto
{
    public int EmployeeId { get; set; }
    public int? DocumentTypeId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? Remarks { get; set; }
}

public class DocumentReplaceDto
{
    public string? DocumentNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? Remarks { get; set; }
}

public class DocumentVerificationDto
{
    [Required]
    public bool Approved { get; set; }

    [MaxLength(512)]
    public string? RejectionReason { get; set; }

    [MaxLength(512)]
    public string? Remarks { get; set; }
}

public class DocumentExpiryDto
{
    public int DocumentId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public string Status { get; set; } = "Expiring Soon"; // Expiring Soon, Expired
}

public class MissingDocumentDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public int DocumentTypeId { get; set; }
    public string RequiredDocumentName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool IsOnboardingMandatory { get; set; }
    public string Status { get; set; } = "Missing"; // Missing, Pending Verification, Rejected, Expired
}

public class DocumentAuditResponseDto
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? UserName { get; set; }
    public string? UserRole { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DocumentFilterQuery
{
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? BranchId { get; set; }
    public int? DocumentTypeId { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? ExpiryStatus { get; set; }
    public string? SearchTerm { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IncludeArchived { get; set; } = false;
}

public class SendReminderRequestDto
{
    public int EmployeeId { get; set; }
    public int? DocumentTypeId { get; set; }
    public string? DocumentName { get; set; }
    public string? CustomMessage { get; set; }
}
