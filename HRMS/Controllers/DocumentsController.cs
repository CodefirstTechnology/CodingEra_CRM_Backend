using HRMS.Authorization;
using HRMS.DTOs;
using HRMS.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
[Route("api/documents")]
[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserAccessor _currentUser;

    public DocumentsController(IDocumentService documentService, ICurrentUserAccessor currentUser)
    {
        _documentService = documentService;
        _currentUser = currentUser;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _documentService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DocumentFilterQuery query, CancellationToken cancellationToken)
    {
        var docs = await _documentService.GetDocumentsAsync(query, cancellationToken);
        return Ok(docs);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var doc = await _documentService.GetDocumentByIdAsync(id, cancellationToken);
        if (doc == null) return NotFound("Document not found or access denied.");
        return Ok(doc);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] DocumentUploadDto dto,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Document file is required.");
        }

        var (result, error, statusCode) = await _documentService.UploadDocumentAsync(dto, file, cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    [HttpPost("{id:int}/replace")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Replace(
        int id,
        [FromForm] DocumentReplaceDto dto,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Replacement file is required.");
        }

        var (result, error, statusCode) = await _documentService.ReplaceDocumentAsync(id, dto, file, cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { error });
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/verify")]
    public async Task<IActionResult> Verify(
        int id,
        [FromBody] DocumentVerificationDto dto,
        CancellationToken cancellationToken)
    {
        var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsVerify);
        if (permissionError != null) return permissionError;

        var (result, error, statusCode) = await _documentService.VerifyDocumentAsync(id, dto, cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { error });
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] DocumentVerificationDto dto,
        CancellationToken cancellationToken)
    {
        var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsReject);
        if (permissionError != null) return permissionError;

        dto.Approved = false;
        var (result, error, statusCode) = await _documentService.VerifyDocumentAsync(id, dto, cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { error });
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        var (success, error, statusCode) = await _documentService.ArchiveDocumentAsync(id, cancellationToken);
        if (!success)
        {
            return StatusCode(statusCode, new { error });
        }

        return Ok(new { message = "Document archived successfully." });
    }

    [HttpGet("{id:int}/preview")]
    public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName, error, statusCode) = await _documentService.GetDocumentFileAsync(id, isDownload: false, cancellationToken);
        if (error != null || content == null)
        {
            return StatusCode(statusCode, new { error });
        }

        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
        return File(content, contentType);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName, error, statusCode) = await _documentService.GetDocumentFileAsync(id, isDownload: true, cancellationToken);
        if (error != null || content == null)
        {
            return StatusCode(statusCode, new { error });
        }

        return File(content, contentType, fileName);
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] int? daysThreshold, CancellationToken cancellationToken)
    {
        var items = await _documentService.GetExpiringDocumentsAsync(daysThreshold, cancellationToken);
        return Ok(items);
    }

    [HttpGet("missing")]
    public async Task<IActionResult> GetMissing(
        [FromQuery] int? departmentId,
        [FromQuery] int? branchId,
        [FromQuery] int? employeeId,
        CancellationToken cancellationToken)
    {
        var items = await _documentService.GetMissingDocumentsAsync(departmentId, branchId, employeeId, cancellationToken);
        return Ok(items);
    }

    [HttpPost("missing/notify")]
    public async Task<IActionResult> SendReminder([FromBody] SendReminderRequestDto dto, CancellationToken cancellationToken)
    {
        var (success, error, statusCode) = await _documentService.SendDocumentReminderAsync(dto, cancellationToken);
        if (!success)
        {
            return StatusCode(statusCode, new { error });
        }

        return Ok(new { message = "Reminder notification sent successfully to employee." });
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit(
        [FromQuery] int? employeeId,
        [FromQuery] int? documentId,
        CancellationToken cancellationToken)
    {
        var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsAudit);
        if (permissionError != null) return permissionError;

        var logs = await _documentService.GetAuditLogsAsync(employeeId, documentId, cancellationToken);
        return Ok(logs);
    }

    // =========================================================================
    // Document Types
    // =========================================================================
    [HttpGet("types")]
    public async Task<IActionResult> GetTypes([FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var types = await _documentService.GetDocumentTypesAsync(includeInactive, cancellationToken);
        return Ok(types);
    }

    [HttpGet("types/{id:int}")]
    public async Task<IActionResult> GetTypeById(int id, CancellationToken cancellationToken)
    {
        var type = await _documentService.GetDocumentTypeByIdAsync(id, cancellationToken);
        if (type == null) return NotFound("Document Type not found.");
        return Ok(type);
    }

    [HttpPost("types")]
    public async Task<IActionResult> CreateType([FromBody] DocumentTypeUpsertDto dto, CancellationToken cancellationToken)
    {
        var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsConfigure);
        if (permissionError != null) return permissionError;

        var (result, error, statusCode) = await _documentService.CreateDocumentTypeAsync(dto, cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { error });
        }

        return CreatedAtAction(nameof(GetTypeById), new { id = result!.Id }, result);
    }

    [HttpPut("types/{id:int}")]
    public async Task<IActionResult> UpdateType(int id, [FromBody] DocumentTypeUpsertDto dto, CancellationToken cancellationToken)
    {
        var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsConfigure);
        if (permissionError != null) return permissionError;

        var (result, error, statusCode) = await _documentService.UpdateDocumentTypeAsync(id, dto, cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { error });
        }

        return Ok(result);
    }

    [HttpDelete("types/{id:int}")]
    public async Task<IActionResult> DeleteType(int id, CancellationToken cancellationToken)
    {
        var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsConfigure);
        if (permissionError != null) return permissionError;

        var (success, error, statusCode) = await _documentService.DeleteDocumentTypeAsync(id, cancellationToken);
        if (!success)
        {
            return StatusCode(statusCode, new { error });
        }

        return NoContent();
    }

    // =========================================================================
    // Document Requirements
    // =========================================================================
    [HttpGet("requirements")]
    public async Task<IActionResult> GetRequirements(CancellationToken cancellationToken)
    {
        var reqs = await _documentService.GetDocumentRequirementsAsync(cancellationToken);
        return Ok(reqs);
    }

    [HttpPost("requirements")]
    public async Task<IActionResult> CreateRequirement([FromBody] DocumentRequirementUpsertDto dto, CancellationToken cancellationToken)
    {
        var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsConfigure);
        if (permissionError != null) return permissionError;

        var (result, error, statusCode) = await _documentService.CreateDocumentRequirementAsync(dto, cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { error });
        }

        return Ok(result);
    }

    [HttpPut("requirements/{id:int}")]
    public async Task<IActionResult> UpdateRequirement(int id, [FromBody] DocumentRequirementUpsertDto dto, CancellationToken cancellationToken)
    {
        var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsConfigure);
        if (permissionError != null) return permissionError;

        var (result, error, statusCode) = await _documentService.UpdateDocumentRequirementAsync(id, dto, cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { error });
        }

        return Ok(result);
    }

    [HttpDelete("requirements/{id:int}")]
    public async Task<IActionResult> DeleteRequirement(int id, CancellationToken cancellationToken)
    {
        var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsConfigure);
        if (permissionError != null) return permissionError;

        var (success, error, statusCode) = await _documentService.DeleteDocumentRequirementAsync(id, cancellationToken);
        if (!success)
        {
            return StatusCode(statusCode, new { error });
        }

        return NoContent();
    }
}
