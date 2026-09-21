using System.Text.Json;
using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class DocumentService : IDocumentService
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuditService _auditService;

    public DocumentService(
        HRMSDbContext context,
        ICurrentUserAccessor currentUser,
        ITenantAccessor tenantAccessor,
        IWebHostEnvironment environment,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantAccessor = tenantAccessor;
        _environment = environment;
        _auditService = auditService;
    }

    private int CurrentTenantId => _tenantAccessor.TenantId.HasValue && _tenantAccessor.TenantId.Value > 0 ? _tenantAccessor.TenantId.Value : 1;

    public async Task<DocumentSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = CurrentTenantId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysLater = today.AddDays(30);

        var docsQuery = _context.EmployeeDocuments
            .AsNoTracking()
            .Include(d => d.DocumentType)
            .Where(d => !d.IsArchived);

        if (!_currentUser.IsAdmin && _currentUser.EmployeeId.HasValue)
        {
            docsQuery = docsQuery.Where(d => d.EmployeeId == _currentUser.EmployeeId.Value);
        }

        var docs = await docsQuery.ToListAsync(cancellationToken);

        var total = docs.Count;
        var verified = docs.Count(d => d.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase));
        var pending = docs.Count(d => d.Status.Equals("Pending Verification", StringComparison.OrdinalIgnoreCase) || d.Status.Equals("Uploaded", StringComparison.OrdinalIgnoreCase) || d.Status.Equals("Resubmission", StringComparison.OrdinalIgnoreCase));
        var rejected = docs.Count(d => d.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase));

        var expired = docs.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate.Value < today);
        var expiringSoon = docs.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate.Value >= today && d.ExpiryDate.Value <= thirtyDaysLater);

        var missingDocs = await GetMissingDocumentsAsync(null, null, _currentUser.IsAdmin ? null : _currentUser.EmployeeId, cancellationToken);

        var catDist = docs
            .GroupBy(d => d.DocumentType?.Category ?? "Other")
            .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var recentLogs = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityName == "EmployeeDocument")
            .OrderByDescending(a => a.CreatedAt)
            .Take(6)
            .ToListAsync(cancellationToken);

        var recentActivities = recentLogs.Select(a => new RecentDocumentActivityDto
        {
            DocumentId = int.TryParse(a.EntityId, out var id) ? id : 0,
            DocumentName = a.EntityId ?? "Document",
            EmployeeName = a.UserName ?? "User",
            Action = a.Action,
            Status = a.Action.Contains("VERIFY", StringComparison.OrdinalIgnoreCase) ? "Verified" :
                     a.Action.Contains("REJECT", StringComparison.OrdinalIgnoreCase) ? "Rejected" : "Uploaded",
            PerformedBy = a.UserName ?? "System",
            Timestamp = a.CreatedAt
        }).ToList();

        return new DocumentSummaryDto
        {
            TotalDocuments = total,
            VerifiedCount = verified,
            PendingVerificationCount = pending,
            RejectedCount = rejected,
            ExpiringSoonCount = expiringSoon,
            ExpiredCount = expired,
            MissingDocumentsCount = missingDocs.Count,
            VerificationRate = total > 0 ? Math.Round((decimal)verified / total * 100m, 1) : 0m,
            CategoryDistribution = catDist,
            RecentActivities = recentActivities
        };
    }

    public async Task<List<EmployeeDocumentResponseDto>> GetDocumentsAsync(DocumentFilterQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.EmployeeDocuments
            .AsNoTracking()
            .Include(d => d.Employee)
                .ThenInclude(e => e!.Department)
            .Include(d => d.Employee)
                .ThenInclude(e => e!.Branch)
            .Include(d => d.DocumentType)
            .AsQueryable();

        if (!query.IncludeArchived)
        {
            dbQuery = dbQuery.Where(d => !d.IsArchived);
        }

        if (!_currentUser.IsAdmin)
        {
            if (!_currentUser.EmployeeId.HasValue) return new List<EmployeeDocumentResponseDto>();
            dbQuery = dbQuery.Where(d => d.EmployeeId == _currentUser.EmployeeId.Value);
        }
        else if (query.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(d => d.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            dbQuery = dbQuery.Where(d => d.Employee != null && d.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId.HasValue)
        {
            dbQuery = dbQuery.Where(d => d.Employee != null && d.Employee.BranchId == query.BranchId.Value);
        }

        if (query.DocumentTypeId.HasValue)
        {
            dbQuery = dbQuery.Where(d => d.DocumentTypeId == query.DocumentTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            dbQuery = dbQuery.Where(d => d.DocumentType != null && d.DocumentType.Category == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            dbQuery = dbQuery.Where(d => d.Status == query.Status);
        }

        if (query.StartDate.HasValue)
        {
            var startUtc = query.StartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            dbQuery = dbQuery.Where(d => d.UploadedAt >= startUtc);
        }

        if (query.EndDate.HasValue)
        {
            var endUtc = query.EndDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            dbQuery = dbQuery.Where(d => d.UploadedAt <= endUtc);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(d =>
                d.DocumentName.ToLower().Contains(term) ||
                (d.DocumentNumber != null && d.DocumentNumber.ToLower().Contains(term)) ||
                (d.Employee != null && d.Employee.FullName.ToLower().Contains(term)) ||
                (d.Employee != null && d.Employee.EmployeeCode != null && d.Employee.EmployeeCode.ToLower().Contains(term)));
        }

        var docs = await dbQuery
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        var docIds = docs.Select(d => d.Id).ToList();
        var versions = await _context.DocumentVersions
            .AsNoTracking()
            .Where(v => docIds.Contains(v.EmployeeDocumentId))
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDays = today.AddDays(30);

        var results = new List<EmployeeDocumentResponseDto>();
        foreach (var d in docs)
        {
            var docVersions = versions.Where(v => v.EmployeeDocumentId == d.Id).Select(MapToVersionDto).ToList();
            var dto = MapToResponseDto(d, docVersions, today, thirtyDays);

            if (!string.IsNullOrWhiteSpace(query.ExpiryStatus))
            {
                if (!dto.ExpiryStatus.Equals(query.ExpiryStatus, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            results.Add(dto);
        }

        return results;
    }

    public async Task<EmployeeDocumentResponseDto?> GetDocumentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var doc = await _context.EmployeeDocuments
            .AsNoTracking()
            .Include(d => d.Employee)
                .ThenInclude(e => e!.Department)
            .Include(d => d.Employee)
                .ThenInclude(e => e!.Branch)
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (doc == null) return null;

        if (!_currentUser.IsAdmin && _currentUser.EmployeeId != doc.EmployeeId)
        {
            return null;
        }

        var versions = await _context.DocumentVersions
            .AsNoTracking()
            .Where(v => v.EmployeeDocumentId == id)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => MapToVersionDto(v))
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDays = today.AddDays(30);

        return MapToResponseDto(doc, versions, today, thirtyDays);
    }

    public async Task<(EmployeeDocumentResponseDto? Result, string? Error, int StatusCode)> UploadDocumentAsync(
        DocumentUploadDto dto,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (dto.EmployeeId <= 0)
        {
            return (null, "Valid Employee ID is required.", StatusCodes.Status400BadRequest);
        }

        if (!_currentUser.IsAdmin && _currentUser.EmployeeId != dto.EmployeeId)
        {
            return (null, "You can only upload documents for yourself.", StatusCodes.Status403Forbidden);
        }

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return (null, "Employee not found.", StatusCodes.Status404NotFound);
        }

        DocumentType? docType = null;
        if (dto.DocumentTypeId.HasValue)
        {
            docType = await _context.DocumentTypes.FirstOrDefaultAsync(dt => dt.Id == dto.DocumentTypeId.Value, cancellationToken);
            if (docType == null)
            {
                return (null, "Document Type not found.", StatusCodes.Status400BadRequest);
            }
        }

        var validationError = ValidateFile(file, docType);
        if (validationError != null)
        {
            return (null, validationError, StatusCodes.Status400BadRequest);
        }

        if (dto.IssueDate.HasValue && dto.ExpiryDate.HasValue && dto.ExpiryDate.Value < dto.IssueDate.Value)
        {
            return (null, "Expiry date cannot be before issue date.", StatusCodes.Status400BadRequest);
        }

        var docName = !string.IsNullOrWhiteSpace(dto.DocumentName)
            ? dto.DocumentName.Trim()
            : (docType?.Name ?? Path.GetFileNameWithoutExtension(file.FileName));

        var relativePath = await SavePhysicalFileAsync(file, employee.Id, cancellationToken);

        var entity = new EmployeeDocument
        {
            TenantId = CurrentTenantId,
            EmployeeId = dto.EmployeeId,
            DocumentTypeId = dto.DocumentTypeId,
            DocumentName = docName,
            DocumentNumber = dto.DocumentNumber?.Trim(),
            IssueDate = dto.IssueDate,
            ExpiryDate = dto.ExpiryDate,
            FilePath = relativePath,
            FileName = Path.GetFileName(file.FileName),
            FileSize = file.Length,
            MimeType = file.ContentType,
            VersionNumber = 1,
            IsCurrent = true,
            Status = docType?.RequiresVerification == false ? "Verified" : "Pending Verification",
            Remarks = dto.Remarks?.Trim(),
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmployeeDocuments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        var version = new DocumentVersion
        {
            TenantId = CurrentTenantId,
            EmployeeDocumentId = entity.Id,
            EmployeeId = entity.EmployeeId,
            DocumentTypeId = entity.DocumentTypeId,
            VersionNumber = 1,
            FilePath = entity.FilePath,
            FileName = entity.FileName,
            FileSize = entity.FileSize,
            MimeType = entity.MimeType,
            DocumentNumber = entity.DocumentNumber,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            Status = entity.Status,
            UploadedByUserId = _currentUser.UserId,
            UploadedByName = _currentUser.FullName,
            UploadedAt = entity.UploadedAt,
            Remarks = entity.Remarks
        };

        _context.DocumentVersions.Add(version);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "DOCUMENT_UPLOADED",
            "EmployeeDocument",
            entity.Id.ToString(),
            new { entity.DocumentName, entity.EmployeeId, entity.VersionNumber, entity.FileName },
            CurrentTenantId,
            cancellationToken);

        var result = await GetDocumentByIdAsync(entity.Id, cancellationToken);
        return (result, null, StatusCodes.Status200OK);
    }

    public async Task<(EmployeeDocumentResponseDto? Result, string? Error, int StatusCode)> ReplaceDocumentAsync(
        int id,
        DocumentReplaceDto dto,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var doc = await _context.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (doc == null)
        {
            return (null, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!_currentUser.IsAdmin && _currentUser.EmployeeId != doc.EmployeeId)
        {
            return (null, "You do not have access to update this document.", StatusCodes.Status403Forbidden);
        }

        DocumentType? docType = null;
        if (doc.DocumentTypeId.HasValue)
        {
            docType = await _context.DocumentTypes.FirstOrDefaultAsync(dt => dt.Id == doc.DocumentTypeId.Value, cancellationToken);
        }

        var validationError = ValidateFile(file, docType);
        if (validationError != null)
        {
            return (null, validationError, StatusCodes.Status400BadRequest);
        }

        if (dto.IssueDate.HasValue && dto.ExpiryDate.HasValue && dto.ExpiryDate.Value < dto.IssueDate.Value)
        {
            return (null, "Expiry date cannot be before issue date.", StatusCodes.Status400BadRequest);
        }

        var relativePath = await SavePhysicalFileAsync(file, doc.EmployeeId, cancellationToken);

        doc.VersionNumber += 1;
        doc.FilePath = relativePath;
        doc.FileName = Path.GetFileName(file.FileName);
        doc.FileSize = file.Length;
        doc.MimeType = file.ContentType;
        if (!string.IsNullOrWhiteSpace(dto.DocumentNumber)) doc.DocumentNumber = dto.DocumentNumber.Trim();
        if (dto.IssueDate.HasValue) doc.IssueDate = dto.IssueDate;
        if (dto.ExpiryDate.HasValue) doc.ExpiryDate = dto.ExpiryDate;
        if (!string.IsNullOrWhiteSpace(dto.Remarks)) doc.Remarks = dto.Remarks.Trim();

        doc.Status = "Pending Verification";
        doc.RejectionReason = null;
        doc.VerifiedByUserId = null;
        doc.VerifiedByName = null;
        doc.VerifiedAt = null;
        doc.UploadedAt = DateTime.UtcNow;
        doc.UpdatedAt = DateTime.UtcNow;

        var version = new DocumentVersion
        {
            TenantId = CurrentTenantId,
            EmployeeDocumentId = doc.Id,
            EmployeeId = doc.EmployeeId,
            DocumentTypeId = doc.DocumentTypeId,
            VersionNumber = doc.VersionNumber,
            FilePath = doc.FilePath,
            FileName = doc.FileName,
            FileSize = doc.FileSize,
            MimeType = doc.MimeType,
            DocumentNumber = doc.DocumentNumber,
            IssueDate = doc.IssueDate,
            ExpiryDate = doc.ExpiryDate,
            Status = doc.Status,
            UploadedByUserId = _currentUser.UserId,
            UploadedByName = _currentUser.FullName,
            UploadedAt = DateTime.UtcNow,
            Remarks = dto.Remarks
        };

        _context.DocumentVersions.Add(version);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "DOCUMENT_REPLACED",
            "EmployeeDocument",
            doc.Id.ToString(),
            new { doc.DocumentName, doc.EmployeeId, NewVersion = doc.VersionNumber, doc.FileName },
            CurrentTenantId,
            cancellationToken);

        var result = await GetDocumentByIdAsync(doc.Id, cancellationToken);
        return (result, null, StatusCodes.Status200OK);
    }

    public async Task<(EmployeeDocumentResponseDto? Result, string? Error, int StatusCode)> VerifyDocumentAsync(
        int id,
        DocumentVerificationDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin)
        {
            return (null, "Only HR Administrators can verify documents.", StatusCodes.Status403Forbidden);
        }

        var doc = await _context.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (doc == null)
        {
            return (null, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (dto.Approved)
        {
            doc.Status = "Verified";
            doc.RejectionReason = null;
            doc.VerifiedByUserId = _currentUser.UserId;
            doc.VerifiedByName = _currentUser.FullName;
            doc.VerifiedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.Remarks)) doc.Remarks = dto.Remarks.Trim();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.RejectionReason))
            {
                return (null, "Rejection reason is mandatory when rejecting a document.", StatusCodes.Status400BadRequest);
            }

            doc.Status = "Rejected";
            doc.RejectionReason = dto.RejectionReason.Trim();
            doc.VerifiedByUserId = _currentUser.UserId;
            doc.VerifiedByName = _currentUser.FullName;
            doc.VerifiedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.Remarks)) doc.Remarks = dto.Remarks.Trim();
        }

        doc.UpdatedAt = DateTime.UtcNow;

        var latestVersion = await _context.DocumentVersions
            .Where(v => v.EmployeeDocumentId == id && v.VersionNumber == doc.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestVersion != null)
        {
            latestVersion.Status = doc.Status;
            latestVersion.RejectionReason = doc.RejectionReason;
            latestVersion.VerifiedByUserId = doc.VerifiedByUserId;
            latestVersion.VerifiedByName = doc.VerifiedByName;
            latestVersion.VerifiedAt = doc.VerifiedAt;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            dto.Approved ? "DOCUMENT_VERIFIED" : "DOCUMENT_REJECTED",
            "EmployeeDocument",
            doc.Id.ToString(),
            new { doc.DocumentName, doc.EmployeeId, doc.Status, doc.RejectionReason, VerifiedBy = _currentUser.FullName },
            CurrentTenantId,
            cancellationToken);

        var result = await GetDocumentByIdAsync(doc.Id, cancellationToken);
        return (result, null, StatusCodes.Status200OK);
    }

    public async Task<(bool Success, string? Error, int StatusCode)> ArchiveDocumentAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin)
        {
            return (false, "Only HR Administrators can archive documents.", StatusCodes.Status403Forbidden);
        }

        var doc = await _context.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (doc == null)
        {
            return (false, "Document not found.", StatusCodes.Status404NotFound);
        }

        doc.IsArchived = true;
        doc.Status = "Archived";
        doc.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "DOCUMENT_ARCHIVED",
            "EmployeeDocument",
            doc.Id.ToString(),
            new { doc.DocumentName, doc.EmployeeId },
            CurrentTenantId,
            cancellationToken);

        return (true, null, StatusCodes.Status200OK);
    }

    public async Task<(byte[]? Content, string ContentType, string FileName, string? Error, int StatusCode)> GetDocumentFileAsync(
        int id,
        bool isDownload = false,
        CancellationToken cancellationToken = default)
    {
        var doc = await _context.EmployeeDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (doc == null)
        {
            return (null, string.Empty, string.Empty, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!_currentUser.IsAdmin && _currentUser.EmployeeId != doc.EmployeeId)
        {
            return (null, string.Empty, string.Empty, "Access denied to this document.", StatusCodes.Status403Forbidden);
        }

        var physicalPath = Path.Combine(_environment.ContentRootPath, doc.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(physicalPath))
        {
            return (null, string.Empty, string.Empty, "Physical document file was not found on server storage.", StatusCodes.Status404NotFound);
        }

        var bytes = await File.ReadAllBytesAsync(physicalPath, cancellationToken);
        var mimeType = doc.MimeType ?? GetMimeType(doc.FileName ?? physicalPath);
        var fileName = doc.FileName ?? Path.GetFileName(physicalPath);

        await _auditService.LogAsync(
            isDownload ? "DOCUMENT_DOWNLOADED" : "DOCUMENT_VIEWED",
            "EmployeeDocument",
            doc.Id.ToString(),
            new { doc.DocumentName, doc.EmployeeId, FileName = fileName },
            CurrentTenantId,
            cancellationToken);

        return (bytes, mimeType, fileName, null, StatusCodes.Status200OK);
    }

    public async Task<List<DocumentExpiryDto>> GetExpiringDocumentsAsync(int? daysThreshold, CancellationToken cancellationToken = default)
    {
        var thresholdDays = daysThreshold ?? 30;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(thresholdDays);

        var query = _context.EmployeeDocuments
            .AsNoTracking()
            .Include(d => d.Employee)
                .ThenInclude(e => e!.Department)
            .Include(d => d.Employee)
                .ThenInclude(e => e!.Branch)
            .Where(d => !d.IsArchived && d.ExpiryDate.HasValue && d.ExpiryDate.Value <= cutoff);

        if (!_currentUser.IsAdmin && _currentUser.EmployeeId.HasValue)
        {
            query = query.Where(d => d.EmployeeId == _currentUser.EmployeeId.Value);
        }

        var docs = await query
            .OrderBy(d => d.ExpiryDate)
            .ToListAsync(cancellationToken);

        return docs.Select(d =>
        {
            var daysRemaining = d.ExpiryDate!.Value.DayNumber - today.DayNumber;
            return new DocumentExpiryDto
            {
                DocumentId = d.Id,
                EmployeeId = d.EmployeeId,
                EmployeeName = d.Employee?.FullName ?? "Unknown",
                EmployeeCode = d.Employee?.EmployeeCode ?? string.Empty,
                DepartmentName = d.Employee?.Department?.Name ?? "General",
                BranchName = d.Employee?.Branch?.Name ?? "Main Office",
                DocumentName = d.DocumentName,
                DocumentNumber = MaskDocumentNumber(d.DocumentNumber),
                ExpiryDate = d.ExpiryDate,
                DaysRemaining = daysRemaining,
                Status = daysRemaining < 0 ? "Expired" : "Expiring Soon"
            };
        }).ToList();
    }

    public async Task<List<MissingDocumentDto>> GetMissingDocumentsAsync(
        int? departmentId,
        int? branchId,
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var empQuery = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Branch)
            .Where(e => e.Status == "Active");

        if (!_currentUser.IsAdmin && _currentUser.EmployeeId.HasValue)
        {
            empQuery = empQuery.Where(e => e.Id == _currentUser.EmployeeId.Value);
        }
        else if (employeeId.HasValue)
        {
            empQuery = empQuery.Where(e => e.Id == employeeId.Value);
        }

        if (departmentId.HasValue) empQuery = empQuery.Where(e => e.DepartmentId == departmentId.Value);
        if (branchId.HasValue) empQuery = empQuery.Where(e => e.BranchId == branchId.Value);

        var employees = await empQuery.ToListAsync(cancellationToken);
        var activeRequirements = await _context.DocumentRequirements
            .AsNoTracking()
            .Include(r => r.DocumentType)
            .Where(r => r.IsActive && r.DocumentType != null && r.DocumentType.IsActive)
            .ToListAsync(cancellationToken);

        var empIds = employees.Select(e => e.Id).ToList();
        var existingDocs = await _context.EmployeeDocuments
            .AsNoTracking()
            .Where(d => empIds.Contains(d.EmployeeId) && !d.IsArchived)
            .ToListAsync(cancellationToken);

        var missingList = new List<MissingDocumentDto>();

        foreach (var emp in employees)
        {
            var empDocs = existingDocs.Where(d => d.EmployeeId == emp.Id).ToList();

            foreach (var req in activeRequirements)
            {
                if (!string.IsNullOrWhiteSpace(req.EmploymentType) && !string.Equals(req.EmploymentType, emp.EmploymentType, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (req.DepartmentId.HasValue && req.DepartmentId.Value != emp.DepartmentId)
                    continue;

                if (req.BranchId.HasValue && req.BranchId.Value != emp.BranchId)
                    continue;

                if (req.DesignationId.HasValue && req.DesignationId.Value != emp.DesignationId)
                    continue;

                if (!string.IsNullOrWhiteSpace(req.Gender) && !string.Equals(req.Gender, emp.Gender, StringComparison.OrdinalIgnoreCase))
                    continue;

                var submittedDoc = empDocs.FirstOrDefault(d => d.DocumentTypeId == req.DocumentTypeId);
                if (submittedDoc == null)
                {
                    missingList.Add(new MissingDocumentDto
                    {
                        EmployeeId = emp.Id,
                        EmployeeName = emp.FullName,
                        EmployeeCode = emp.EmployeeCode ?? string.Empty,
                        DepartmentName = emp.Department?.Name ?? "General",
                        BranchName = emp.Branch?.Name ?? "Main Office",
                        EmploymentType = emp.EmploymentType ?? "Full Time",
                        DocumentTypeId = req.DocumentTypeId,
                        RequiredDocumentName = req.DocumentType?.Name ?? "Required Document",
                        Category = req.DocumentType?.Category ?? "Identity",
                        IsMandatory = req.IsMandatory,
                        IsOnboardingMandatory = req.IsOnboardingMandatory,
                        Status = "Missing"
                    });
                }
                else if (submittedDoc.Status == "Rejected" || submittedDoc.Status == "Pending Verification")
                {
                    missingList.Add(new MissingDocumentDto
                    {
                        EmployeeId = emp.Id,
                        EmployeeName = emp.FullName,
                        EmployeeCode = emp.EmployeeCode ?? string.Empty,
                        DepartmentName = emp.Department?.Name ?? "General",
                        BranchName = emp.Branch?.Name ?? "Main Office",
                        EmploymentType = emp.EmploymentType ?? "Full Time",
                        DocumentTypeId = req.DocumentTypeId,
                        RequiredDocumentName = req.DocumentType?.Name ?? "Required Document",
                        Category = req.DocumentType?.Category ?? "Identity",
                        IsMandatory = req.IsMandatory,
                        IsOnboardingMandatory = req.IsOnboardingMandatory,
                        Status = submittedDoc.Status
                    });
                }
            }
        }

        return missingList;
    }

    public async Task<(bool Success, string? Error, int StatusCode)> SendDocumentReminderAsync(SendReminderRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin)
        {
            return (false, "Only HR Administrators can send document reminders.", StatusCodes.Status403Forbidden);
        }

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return (false, "Employee not found.", StatusCodes.Status404NotFound);
        }

        var message = dto.CustomMessage ?? $"Reminder: Please upload required document '{dto.DocumentName ?? "Pending Document"}' for verification.";

        var notification = new LeaveNotification
        {
            TenantId = CurrentTenantId,
            EmployeeId = employee.Id,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.LeaveNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "EXPIRY_REMINDER_SENT",
            "EmployeeDocument",
            dto.DocumentTypeId?.ToString(),
            new { dto.EmployeeId, employee.FullName, dto.DocumentName, Message = message },
            CurrentTenantId,
            cancellationToken);

        return (true, null, StatusCodes.Status200OK);
    }

    public async Task<List<DocumentAuditResponseDto>> GetAuditLogsAsync(int? employeeId, int? documentId, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityName == "EmployeeDocument" || a.EntityName == "DocumentType" || a.EntityName == "DocumentRequirement");

        if (documentId.HasValue)
        {
            var idStr = documentId.Value.ToString();
            query = query.Where(a => a.EntityId == idStr);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return logs.Select(a => new DocumentAuditResponseDto
        {
            Id = a.Id,
            TenantId = a.TenantId,
            Action = a.Action,
            EntityName = a.EntityName,
            EntityId = a.EntityId,
            UserName = a.UserName,
            UserRole = a.UserRole,
            Details = a.Details,
            IpAddress = a.IpAddress,
            CreatedAt = a.CreatedAt
        }).ToList();
    }

    // =========================================================================
    // Document Types CRUD
    // =========================================================================
    public async Task<List<DocumentTypeDto>> GetDocumentTypesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentTypes.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(dt => dt.IsActive);
        }

        var types = await query.OrderBy(dt => dt.Name).ToListAsync(cancellationToken);
        var typeIds = types.Select(t => t.Id).ToList();

        var docCounts = await _context.EmployeeDocuments
            .AsNoTracking()
            .Where(d => d.DocumentTypeId.HasValue && typeIds.Contains(d.DocumentTypeId.Value) && !d.IsArchived)
            .GroupBy(d => d.DocumentTypeId!.Value)
            .Select(g => new { TypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TypeId, x => x.Count, cancellationToken);

        return types.Select(dt => new DocumentTypeDto
        {
            Id = dt.Id,
            TenantId = dt.TenantId,
            Name = dt.Name,
            Code = dt.Code,
            Category = dt.Category,
            Description = dt.Description,
            IsRequired = dt.IsRequired,
            IsMandatoryDuringOnboarding = dt.IsMandatoryDuringOnboarding,
            RequiresVerification = dt.RequiresVerification,
            HasExpiry = dt.HasExpiry,
            DefaultValidityPeriodMonths = dt.DefaultValidityPeriodMonths,
            AllowedFileTypes = dt.AllowedFileTypes,
            MaximumFileSizeMb = dt.MaximumFileSizeMb,
            Status = dt.Status,
            IsActive = dt.IsActive,
            ActiveDocumentsCount = docCounts.TryGetValue(dt.Id, out var count) ? count : 0,
            CreatedAt = dt.CreatedAt,
            UpdatedAt = dt.UpdatedAt
        }).ToList();
    }

    public async Task<DocumentTypeDto?> GetDocumentTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dt = await _context.DocumentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (dt == null) return null;

        var count = await _context.EmployeeDocuments.CountAsync(d => d.DocumentTypeId == id && !d.IsArchived, cancellationToken);

        return new DocumentTypeDto
        {
            Id = dt.Id,
            TenantId = dt.TenantId,
            Name = dt.Name,
            Code = dt.Code,
            Category = dt.Category,
            Description = dt.Description,
            IsRequired = dt.IsRequired,
            IsMandatoryDuringOnboarding = dt.IsMandatoryDuringOnboarding,
            RequiresVerification = dt.RequiresVerification,
            HasExpiry = dt.HasExpiry,
            DefaultValidityPeriodMonths = dt.DefaultValidityPeriodMonths,
            AllowedFileTypes = dt.AllowedFileTypes,
            MaximumFileSizeMb = dt.MaximumFileSizeMb,
            Status = dt.Status,
            IsActive = dt.IsActive,
            ActiveDocumentsCount = count,
            CreatedAt = dt.CreatedAt,
            UpdatedAt = dt.UpdatedAt
        };
    }

    public async Task<(DocumentTypeDto? Result, string? Error, int StatusCode)> CreateDocumentTypeAsync(DocumentTypeUpsertDto dto, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin) return (null, "Forbidden", StatusCodes.Status403Forbidden);

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _context.DocumentTypes.AnyAsync(x => x.Code == code, cancellationToken))
        {
            return (null, $"Document type with code '{code}' already exists.", StatusCodes.Status409Conflict);
        }

        var entity = new DocumentType
        {
            TenantId = CurrentTenantId,
            Name = dto.Name.Trim(),
            Code = code,
            Category = dto.Category.Trim(),
            Description = dto.Description?.Trim(),
            IsRequired = dto.IsRequired,
            IsMandatoryDuringOnboarding = dto.IsMandatoryDuringOnboarding,
            RequiresVerification = dto.RequiresVerification,
            HasExpiry = dto.HasExpiry,
            DefaultValidityPeriodMonths = dto.DefaultValidityPeriodMonths,
            AllowedFileTypes = string.IsNullOrWhiteSpace(dto.AllowedFileTypes) ? "pdf,jpg,jpeg,png" : dto.AllowedFileTypes.Trim().ToLowerInvariant(),
            MaximumFileSizeMb = dto.MaximumFileSizeMb > 0 ? dto.MaximumFileSizeMb : 5,
            Status = dto.Status ?? "Active",
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("DOCUMENT_TYPE_CREATED", "DocumentType", entity.Id.ToString(), new { entity.Name, entity.Code }, CurrentTenantId, cancellationToken);
        var result = await GetDocumentTypeByIdAsync(entity.Id, cancellationToken);
        return (result, null, StatusCodes.Status200OK);
    }

    public async Task<(DocumentTypeDto? Result, string? Error, int StatusCode)> UpdateDocumentTypeAsync(int id, DocumentTypeUpsertDto dto, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin) return (null, "Forbidden", StatusCodes.Status403Forbidden);

        var entity = await _context.DocumentTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (null, "Document Type not found.", StatusCodes.Status404NotFound);

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _context.DocumentTypes.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
        {
            return (null, $"Document type with code '{code}' already exists.", StatusCodes.Status409Conflict);
        }

        entity.Name = dto.Name.Trim();
        entity.Code = code;
        entity.Category = dto.Category.Trim();
        entity.Description = dto.Description?.Trim();
        entity.IsRequired = dto.IsRequired;
        entity.IsMandatoryDuringOnboarding = dto.IsMandatoryDuringOnboarding;
        entity.RequiresVerification = dto.RequiresVerification;
        entity.HasExpiry = dto.HasExpiry;
        entity.DefaultValidityPeriodMonths = dto.DefaultValidityPeriodMonths;
        entity.AllowedFileTypes = string.IsNullOrWhiteSpace(dto.AllowedFileTypes) ? "pdf,jpg,jpeg,png" : dto.AllowedFileTypes.Trim().ToLowerInvariant();
        entity.MaximumFileSizeMb = dto.MaximumFileSizeMb > 0 ? dto.MaximumFileSizeMb : 5;
        entity.Status = dto.Status ?? "Active";
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("DOCUMENT_TYPE_UPDATED", "DocumentType", entity.Id.ToString(), new { entity.Name, entity.Code }, CurrentTenantId, cancellationToken);
        var result = await GetDocumentTypeByIdAsync(entity.Id, cancellationToken);
        return (result, null, StatusCodes.Status200OK);
    }

    public async Task<(bool Success, string? Error, int StatusCode)> DeleteDocumentTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin) return (false, "Forbidden", StatusCodes.Status403Forbidden);

        var entity = await _context.DocumentTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Document Type not found.", StatusCodes.Status404NotFound);

        if (await _context.EmployeeDocuments.AnyAsync(d => d.DocumentTypeId == id && !d.IsArchived, cancellationToken))
        {
            // Soft deactivate
            entity.IsActive = false;
            entity.Status = "Inactive";
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (true, null, StatusCodes.Status200OK);
        }

        _context.DocumentTypes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("DOCUMENT_TYPE_DELETED", "DocumentType", id.ToString(), new { entity.Name, entity.Code }, CurrentTenantId, cancellationToken);
        return (true, null, StatusCodes.Status200OK);
    }

    // =========================================================================
    // Document Requirements CRUD
    // =========================================================================
    public async Task<List<DocumentRequirementDto>> GetDocumentRequirementsAsync(CancellationToken cancellationToken = default)
    {
        var reqs = await _context.DocumentRequirements
            .AsNoTracking()
            .Include(r => r.DocumentType)
            .Include(r => r.Department)
            .Include(r => r.Designation)
            .Include(r => r.Branch)
            .OrderBy(r => r.DocumentType != null ? r.DocumentType.Name : string.Empty)
            .ToListAsync(cancellationToken);

        return reqs.Select(r => new DocumentRequirementDto
        {
            Id = r.Id,
            TenantId = r.TenantId,
            DocumentTypeId = r.DocumentTypeId,
            DocumentTypeName = r.DocumentType?.Name ?? "Unknown",
            DocumentTypeCode = r.DocumentType?.Code ?? string.Empty,
            Category = r.DocumentType?.Category ?? "Other",
            EmploymentType = r.EmploymentType,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.Department?.Name,
            DesignationId = r.DesignationId,
            DesignationName = r.Designation?.Name,
            BranchId = r.BranchId,
            BranchName = r.Branch?.Name,
            Gender = r.Gender,
            IsMandatory = r.IsMandatory,
            IsOnboardingMandatory = r.IsOnboardingMandatory,
            IsActive = r.IsActive,
            Remarks = r.Remarks,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<(DocumentRequirementDto? Result, string? Error, int StatusCode)> CreateDocumentRequirementAsync(DocumentRequirementUpsertDto dto, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin) return (null, "Forbidden", StatusCodes.Status403Forbidden);

        if (!await _context.DocumentTypes.AnyAsync(dt => dt.Id == dto.DocumentTypeId, cancellationToken))
        {
            return (null, "Document Type not found.", StatusCodes.Status400BadRequest);
        }

        var entity = new DocumentRequirement
        {
            TenantId = CurrentTenantId,
            DocumentTypeId = dto.DocumentTypeId,
            EmploymentType = dto.EmploymentType,
            DepartmentId = dto.DepartmentId,
            DesignationId = dto.DesignationId,
            BranchId = dto.BranchId,
            Gender = dto.Gender,
            IsMandatory = dto.IsMandatory,
            IsOnboardingMandatory = dto.IsOnboardingMandatory,
            IsActive = dto.IsActive,
            Remarks = dto.Remarks?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentRequirements.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("DOCUMENT_REQUIREMENT_CREATED", "DocumentRequirement", entity.Id.ToString(), new { entity.DocumentTypeId, entity.EmploymentType }, CurrentTenantId, cancellationToken);
        var reqs = await GetDocumentRequirementsAsync(cancellationToken);
        return (reqs.FirstOrDefault(r => r.Id == entity.Id), null, StatusCodes.Status200OK);
    }

    public async Task<(DocumentRequirementDto? Result, string? Error, int StatusCode)> UpdateDocumentRequirementAsync(int id, DocumentRequirementUpsertDto dto, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin) return (null, "Forbidden", StatusCodes.Status403Forbidden);

        var entity = await _context.DocumentRequirements.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity == null) return (null, "Document Requirement not found.", StatusCodes.Status404NotFound);

        entity.DocumentTypeId = dto.DocumentTypeId;
        entity.EmploymentType = dto.EmploymentType;
        entity.DepartmentId = dto.DepartmentId;
        entity.DesignationId = dto.DesignationId;
        entity.BranchId = dto.BranchId;
        entity.Gender = dto.Gender;
        entity.IsMandatory = dto.IsMandatory;
        entity.IsOnboardingMandatory = dto.IsOnboardingMandatory;
        entity.IsActive = dto.IsActive;
        entity.Remarks = dto.Remarks?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("DOCUMENT_REQUIREMENT_UPDATED", "DocumentRequirement", entity.Id.ToString(), new { entity.DocumentTypeId, entity.EmploymentType }, CurrentTenantId, cancellationToken);
        var reqs = await GetDocumentRequirementsAsync(cancellationToken);
        return (reqs.FirstOrDefault(r => r.Id == entity.Id), null, StatusCodes.Status200OK);
    }

    public async Task<(bool Success, string? Error, int StatusCode)> DeleteDocumentRequirementAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin) return (false, "Forbidden", StatusCodes.Status403Forbidden);

        var entity = await _context.DocumentRequirements.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity == null) return (false, "Document Requirement not found.", StatusCodes.Status404NotFound);

        _context.DocumentRequirements.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("DOCUMENT_REQUIREMENT_DELETED", "DocumentRequirement", id.ToString(), null, CurrentTenantId, cancellationToken);
        return (true, null, StatusCodes.Status200OK);
    }

    // =========================================================================
    // Helpers
    // =========================================================================
    private string? ValidateFile(IFormFile file, DocumentType? docType)
    {
        if (file == null || file.Length == 0)
        {
            return "File is empty or not provided.";
        }

        var maxMb = docType?.MaximumFileSizeMb ?? 10;
        if (file.Length > maxMb * 1024L * 1024L)
        {
            return $"File size exceeds the maximum permitted size of {maxMb} MB.";
        }

        var allowedExtensions = (docType?.AllowedFileTypes ?? "pdf,jpg,jpeg,png")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
        {
            return $"File format '{ext}' is not allowed. Supported formats: {string.Join(", ", allowedExtensions)}.";
        }

        return null;
    }

    private async Task<string> SavePhysicalFileAsync(IFormFile file, int employeeId, CancellationToken cancellationToken)
    {
        var tenantFolder = $"tenant_{CurrentTenantId}";
        var employeeFolder = $"emp_{employeeId}";
        var targetDir = Path.Combine(_environment.ContentRootPath, "Uploads", "documents", tenantFolder, employeeFolder);
        Directory.CreateDirectory(targetDir);

        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(targetDir, uniqueFileName);

        await using (var stream = File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return Path.Combine("Uploads", "documents", tenantFolder, employeeFolder, uniqueFileName).Replace('\\', '/');
    }

    private static EmployeeDocumentResponseDto MapToResponseDto(EmployeeDocument d, List<DocumentVersionDto> versions, DateOnly today, DateOnly thirtyDays)
    {
        int? daysRemaining = null;
        string expiryStatus = "No Expiry";

        if (d.ExpiryDate.HasValue)
        {
            daysRemaining = d.ExpiryDate.Value.DayNumber - today.DayNumber;
            if (daysRemaining < 0)
            {
                expiryStatus = "Expired";
            }
            else if (d.ExpiryDate.Value <= thirtyDays)
            {
                expiryStatus = "Expiring Soon";
            }
            else
            {
                expiryStatus = "Valid";
            }
        }

        return new EmployeeDocumentResponseDto
        {
            Id = d.Id,
            TenantId = d.TenantId,
            EmployeeId = d.EmployeeId,
            EmployeeName = d.Employee?.FullName ?? "Unknown",
            EmployeeCode = d.Employee?.EmployeeCode ?? string.Empty,
            DepartmentName = d.Employee?.Department?.Name ?? "General",
            BranchName = d.Employee?.Branch?.Name ?? "Main Office",
            DocumentTypeId = d.DocumentTypeId,
            DocumentTypeName = d.DocumentType?.Name ?? d.DocumentName,
            DocumentTypeCode = d.DocumentType?.Code ?? string.Empty,
            Category = d.DocumentType?.Category ?? "Other",
            DocumentName = d.DocumentName,
            DocumentNumber = d.DocumentNumber,
            MaskedDocumentNumber = MaskDocumentNumber(d.DocumentNumber),
            IssueDate = d.IssueDate,
            ExpiryDate = d.ExpiryDate,
            DaysUntilExpiry = daysRemaining,
            ExpiryStatus = expiryStatus,
            FilePath = d.FilePath,
            FileName = d.FileName,
            FileSize = d.FileSize,
            FileSizeFormatted = FormatFileSize(d.FileSize),
            MimeType = d.MimeType,
            VersionNumber = d.VersionNumber,
            IsCurrent = d.IsCurrent,
            Status = d.Status,
            RejectionReason = d.RejectionReason,
            VerifiedByUserId = d.VerifiedByUserId,
            VerifiedByName = d.VerifiedByName,
            VerifiedAt = d.VerifiedAt,
            Remarks = d.Remarks,
            IsArchived = d.IsArchived,
            UploadedAt = d.UploadedAt,
            Versions = versions
        };
    }

    private static DocumentVersionDto MapToVersionDto(DocumentVersion v)
    {
        return new DocumentVersionDto
        {
            Id = v.Id,
            VersionNumber = v.VersionNumber,
            FileName = v.FileName,
            FileSize = v.FileSize,
            DocumentNumber = MaskDocumentNumber(v.DocumentNumber),
            IssueDate = v.IssueDate,
            ExpiryDate = v.ExpiryDate,
            Status = v.Status,
            RejectionReason = v.RejectionReason,
            UploadedByName = v.UploadedByName,
            UploadedAt = v.UploadedAt,
            VerifiedByName = v.VerifiedByName,
            VerifiedAt = v.VerifiedAt,
            Remarks = v.Remarks
        };
    }

    private static string? MaskDocumentNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number)) return null;
        var trimmed = number.Trim();
        if (trimmed.Length <= 4) return trimmed;
        var suffix = trimmed[^4..];
        return new string('X', trimmed.Length - 4) + suffix;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] units = { "B", "KB", "MB", "GB" };
        int digitGroups = (int)(Math.Log10(bytes) / Math.Log10(1024));
        if (digitGroups >= units.Length) digitGroups = units.Length - 1;
        return $"{bytes / Math.Pow(1024, digitGroups):F1} {units[digitGroups]}";
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}
