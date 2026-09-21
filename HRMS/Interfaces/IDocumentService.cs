using HRMS.DTOs;
using Microsoft.AspNetCore.Http;

namespace HRMS.Interfaces;

public interface IDocumentService
{
    Task<DocumentSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<EmployeeDocumentResponseDto>> GetDocumentsAsync(DocumentFilterQuery query, CancellationToken cancellationToken = default);
    Task<EmployeeDocumentResponseDto?> GetDocumentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(EmployeeDocumentResponseDto? Result, string? Error, int StatusCode)> UploadDocumentAsync(DocumentUploadDto dto, IFormFile file, CancellationToken cancellationToken = default);
    Task<(EmployeeDocumentResponseDto? Result, string? Error, int StatusCode)> ReplaceDocumentAsync(int id, DocumentReplaceDto dto, IFormFile file, CancellationToken cancellationToken = default);
    Task<(EmployeeDocumentResponseDto? Result, string? Error, int StatusCode)> VerifyDocumentAsync(int id, DocumentVerificationDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int StatusCode)> ArchiveDocumentAsync(int id, CancellationToken cancellationToken = default);
    Task<(byte[]? Content, string ContentType, string FileName, string? Error, int StatusCode)> GetDocumentFileAsync(int id, bool isDownload = false, CancellationToken cancellationToken = default);
    Task<List<DocumentExpiryDto>> GetExpiringDocumentsAsync(int? daysThreshold, CancellationToken cancellationToken = default);
    Task<List<MissingDocumentDto>> GetMissingDocumentsAsync(int? departmentId, int? branchId, int? employeeId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int StatusCode)> SendDocumentReminderAsync(SendReminderRequestDto dto, CancellationToken cancellationToken = default);
    Task<List<DocumentAuditResponseDto>> GetAuditLogsAsync(int? employeeId, int? documentId, CancellationToken cancellationToken = default);

    // Document Types
    Task<List<DocumentTypeDto>> GetDocumentTypesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<DocumentTypeDto?> GetDocumentTypeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(DocumentTypeDto? Result, string? Error, int StatusCode)> CreateDocumentTypeAsync(DocumentTypeUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(DocumentTypeDto? Result, string? Error, int StatusCode)> UpdateDocumentTypeAsync(int id, DocumentTypeUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int StatusCode)> DeleteDocumentTypeAsync(int id, CancellationToken cancellationToken = default);

    // Document Requirements
    Task<List<DocumentRequirementDto>> GetDocumentRequirementsAsync(CancellationToken cancellationToken = default);
    Task<(DocumentRequirementDto? Result, string? Error, int StatusCode)> CreateDocumentRequirementAsync(DocumentRequirementUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(DocumentRequirementDto? Result, string? Error, int StatusCode)> UpdateDocumentRequirementAsync(int id, DocumentRequirementUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int StatusCode)> DeleteDocumentRequirementAsync(int id, CancellationToken cancellationToken = default);
}
