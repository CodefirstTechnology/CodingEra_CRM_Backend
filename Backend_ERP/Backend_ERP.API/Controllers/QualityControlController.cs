using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/quality-control")]
    [ApiController]
    public class QualityControlController : ControllerBase
    {
        private readonly IQualityControlService _qcService;

        public QualityControlController(IQualityControlService qcService)
        {
            _qcService = qcService;
        }

        // ── Incoming Material Inspection ──

        [HttpGet("incoming")]
        public async Task<ActionResult<PagedResult<IncomingListItemDto>>> GetIncomingInspections(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _qcService.GetIncomingInspectionsAsync(new ListQueryDto { Search = search, Status = status, Page = page, PageSize = pageSize }, cancellationToken);
            return Ok(result);
        }

        [HttpGet("incoming/dashboard")]
        public async Task<ActionResult<IncomingDashboardDto>> GetIncomingDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetIncomingDashboardAsync(cancellationToken));
        }

        [HttpGet("incoming/{id:int}")]
        public async Task<ActionResult<IncomingDto>> GetIncomingById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _qcService.GetIncomingByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("incoming")]
        public async Task<ActionResult<IncomingDto>> CreateIncoming([FromBody] IncomingCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var created = await _qcService.CreateIncomingAsync(request, ResolveUser(userId), cancellationToken);
            return CreatedAtAction(nameof(GetIncomingById), new { id = created.Id, userId }, created);
        }

        [HttpPut("incoming/{id:int}")]
        public async Task<ActionResult<IncomingDto>> UpdateIncoming(int id, [FromBody] IncomingCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.UpdateIncomingAsync(id, request, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("incoming/{id:int}")]
        public async Task<ActionResult> DeleteIncoming(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var ok = await _qcService.DeleteIncomingAsync(id, ResolveUser(userId), cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("incoming/{id:int}/duplicate")]
        public async Task<ActionResult<IncomingDto>> DuplicateIncoming(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var dup = await _qcService.DuplicateIncomingAsync(id, ResolveUser(userId), cancellationToken);
            return dup is null ? NotFound() : Ok(dup);
        }

        [HttpPost("incoming/{id:int}/submit")]
        public async Task<ActionResult<IncomingDto>> SubmitIncoming(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.SubmitIncomingAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("incoming/{id:int}/approve")]
        public async Task<ActionResult<IncomingDto>> ApproveIncoming(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.ApproveIncomingAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("incoming/{id:int}/reject")]
        public async Task<ActionResult<IncomingDto>> RejectIncoming(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.RejectIncomingAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("incoming/{id:int}/close")]
        public async Task<ActionResult<IncomingDto>> CloseIncoming(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.CloseIncomingAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("incoming/{id:int}/record-inspection")]
        public async Task<ActionResult<IncomingDto>> RecordInspection(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.RecordInspectionAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        // ── In-Process QC ──

        [HttpGet("in-process")]
        public async Task<ActionResult<PagedResult<InProcessListItemDto>>> GetInProcessChecks([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? userId = null, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetInProcessChecksAsync(new ListQueryDto { Search = search, Status = status, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("in-process/dashboard")]
        public async Task<ActionResult<InProcessDashboardDto>> GetInProcessDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetInProcessDashboardAsync(cancellationToken));
        }

        [HttpGet("in-process/{id:int}")]
        public async Task<ActionResult<InProcessDto>> GetInProcessById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _qcService.GetInProcessByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("in-process")]
        public async Task<ActionResult<InProcessDto>> CreateInProcess([FromBody] InProcessCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var created = await _qcService.CreateInProcessAsync(request, ResolveUser(userId), cancellationToken);
            return CreatedAtAction(nameof(GetInProcessById), new { id = created.Id, userId }, created);
        }

        [HttpPut("in-process/{id:int}")]
        public async Task<ActionResult<InProcessDto>> UpdateInProcess(int id, [FromBody] InProcessCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.UpdateInProcessAsync(id, request, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("in-process/{id:int}")]
        public async Task<ActionResult> DeleteInProcess(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var ok = await _qcService.DeleteInProcessAsync(id, ResolveUser(userId), cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("in-process/{id:int}/start")]
        public async Task<ActionResult<InProcessDto>> StartInProcess(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.StartInProcessAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("in-process/{id:int}/record-result")]
        public async Task<ActionResult<InProcessDto>> RecordInProcessResult(int id, [FromBody] RecordResultRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.RecordInProcessResultAsync(id, payload?.ActualValue, payload?.Result, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("in-process/{id:int}/pass")]
        public async Task<ActionResult<InProcessDto>> PassInProcess(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.PassInProcessAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("in-process/{id:int}/fail")]
        public async Task<ActionResult<InProcessDto>> FailInProcess(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.FailInProcessAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("in-process/{id:int}/close")]
        public async Task<ActionResult<InProcessDto>> CloseInProcess(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.CloseInProcessAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        // ── Final Inspection ──

        [HttpGet("final")]
        public async Task<ActionResult<PagedResult<FinalListItemDto>>> GetFinalInspections([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? userId = null, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetFinalInspectionsAsync(new ListQueryDto { Search = search, Status = status, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("final/dashboard")]
        public async Task<ActionResult<FinalDashboardDto>> GetFinalDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetFinalDashboardAsync(cancellationToken));
        }

        [HttpGet("final/{id:int}")]
        public async Task<ActionResult<FinalDto>> GetFinalById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _qcService.GetFinalByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("final")]
        public async Task<ActionResult<FinalDto>> CreateFinal([FromBody] FinalCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var created = await _qcService.CreateFinalAsync(request, ResolveUser(userId), cancellationToken);
            return CreatedAtAction(nameof(GetFinalById), new { id = created.Id, userId }, created);
        }

        [HttpPut("final/{id:int}")]
        public async Task<ActionResult<FinalDto>> UpdateFinal(int id, [FromBody] FinalCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.UpdateFinalAsync(id, request, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("final/{id:int}")]
        public async Task<ActionResult> DeleteFinal(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var ok = await _qcService.DeleteFinalAsync(id, ResolveUser(userId), cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("final/{id:int}/submit")]
        public async Task<ActionResult<FinalDto>> SubmitFinal(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.SubmitFinalAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("final/{id:int}/approve")]
        public async Task<ActionResult<FinalDto>> ApproveFinal(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.ApproveFinalAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("final/{id:int}/reject")]
        public async Task<ActionResult<FinalDto>> RejectFinal(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.RejectFinalAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("final/{id:int}/close")]
        public async Task<ActionResult<FinalDto>> CloseFinal(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.CloseFinalAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("final/{id:int}/generate-certificate")]
        public async Task<ActionResult<CertificateDto>> GenerateCertificate(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var cert = await _qcService.GenerateCertificateAsync(id, ResolveUser(userId), cancellationToken);
            return Ok(cert);
        }

        [HttpPost("final/{id:int}/generate-load-report")]
        public async Task<ActionResult<LoadTestDto>> GenerateLoadReport(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var rpt = await _qcService.GenerateLoadReportAsync(id, ResolveUser(userId), cancellationToken);
            return Ok(rpt);
        }

        // ── Load Test ──

        [HttpGet("load-tests")]
        public async Task<ActionResult<PagedResult<LoadTestListItemDto>>> GetLoadTests([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? userId = null, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetLoadTestsAsync(new ListQueryDto { Search = search, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("load-tests/dashboard")]
        public async Task<ActionResult<LoadTestDashboardDto>> GetLoadTestDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetLoadTestDashboardAsync(cancellationToken));
        }

        [HttpGet("load-tests/{id:int}")]
        public async Task<ActionResult<LoadTestDto>> GetLoadTestById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _qcService.GetLoadTestByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("load-tests")]
        public async Task<ActionResult<LoadTestDto>> CreateLoadTest([FromBody] LoadTestCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var created = await _qcService.CreateLoadTestAsync(request, ResolveUser(userId), cancellationToken);
            return CreatedAtAction(nameof(GetLoadTestById), new { id = created.Id, userId }, created);
        }

        // ── Test Certificate ──

        [HttpGet("certificates")]
        public async Task<ActionResult<PagedResult<CertificateListItemDto>>> GetCertificates([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? userId = null, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetCertificatesAsync(new ListQueryDto { Search = search, Status = status, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("certificates/dashboard")]
        public async Task<ActionResult<CertificateDashboardDto>> GetCertificateDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetCertificateDashboardAsync(cancellationToken));
        }

        [HttpGet("certificates/{id:int}")]
        public async Task<ActionResult<CertificateDto>> GetCertificateById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _qcService.GetCertificateByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("certificates")]
        public async Task<ActionResult<CertificateDto>> CreateCertificate([FromBody] CertificateCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var created = await _qcService.CreateCertificateAsync(request, ResolveUser(userId), cancellationToken);
            return CreatedAtAction(nameof(GetCertificateById), new { id = created.Id, userId }, created);
        }

        [HttpPut("certificates/{id:int}")]
        public async Task<ActionResult<CertificateDto>> UpdateCertificate(int id, [FromBody] CertificateCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.UpdateCertificateAsync(id, request, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("certificates/{id:int}")]
        public async Task<ActionResult> DeleteCertificate(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var ok = await _qcService.DeleteCertificateAsync(id, ResolveUser(userId), cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("certificates/{id:int}/duplicate")]
        public async Task<ActionResult<CertificateDto>> DuplicateCertificate(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var dup = await _qcService.DuplicateCertificateAsync(id, ResolveUser(userId), cancellationToken);
            return dup is null ? NotFound() : Ok(dup);
        }

        [HttpPost("certificates/{id:int}/approve")]
        public async Task<ActionResult<CertificateDto>> ApproveCertificate(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.ApproveCertificateAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("certificates/{id:int}/issue")]
        public async Task<ActionResult<CertificateDto>> IssueCertificate(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.IssueCertificateAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("certificates/{id:int}/cancel")]
        public async Task<ActionResult<CertificateDto>> CancelCertificate(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.CancelCertificateAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        // ── Rejection Analysis ──

        [HttpGet("rejections")]
        public async Task<ActionResult<PagedResult<RejectionListItemDto>>> GetRejections([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? userId = null, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetRejectionsAsync(new ListQueryDto { Search = search, Status = status, Page = page, PageSize = pageSize }, cancellationToken));
        }

        [HttpGet("rejections/dashboard")]
        public async Task<ActionResult<RejectionDashboardDto>> GetRejectionDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetRejectionDashboardAsync(cancellationToken));
        }

        [HttpGet("rejections/report")]
        public async Task<ActionResult<RejectionReportDto>> GetRejectionReport([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _qcService.GetRejectionReportAsync(cancellationToken));
        }

        [HttpGet("rejections/{id:int}")]
        public async Task<ActionResult<RejectionDto>> GetRejectionById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _qcService.GetRejectionByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("rejections")]
        public async Task<ActionResult<RejectionDto>> RecordRejection([FromBody] RejectionCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var created = await _qcService.RecordRejectionAsync(request, ResolveUser(userId), cancellationToken);
            return CreatedAtAction(nameof(GetRejectionById), new { id = created.Id, userId }, created);
        }

        [HttpPost("rejections/{id:int}/start")]
        public async Task<ActionResult<RejectionDto>> StartRejectionAnalysis(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.StartRejectionAnalysisAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("rejections/{id:int}/close")]
        public async Task<ActionResult<RejectionDto>> CloseRejection(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _qcService.CloseRejectionAsync(id, payload, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        private static string ResolveUser(int? userId) => userId is > 0 ? userId.Value.ToString() : "system";
    }

    public class RecordResultRequestDto : StatusActionRequestDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("actualValue")]
        public string? ActualValue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public string? Result { get; set; }
    }
}
