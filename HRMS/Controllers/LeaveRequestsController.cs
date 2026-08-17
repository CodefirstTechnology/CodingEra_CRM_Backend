using HRMS.Authorization;
using HRMS.DTOs;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Route("api/leave-requests")]
[ApiController]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly ICurrentUserAccessor _currentUser;

    public LeaveRequestsController(ILeaveService leaveService, ICurrentUserAccessor currentUser)
    {
        _leaveService = leaveService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? employeeId,
        [FromQuery] int? departmentId,
        [FromQuery] int? leaveTypeId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var items = await _leaveService.GetRequestsAsync(
            employeeId, departmentId, leaveTypeId, status, fromDate, toDate, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _leaveService.GetRequestByIdAsync(id, cancellationToken);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeaveApplyDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (result, error, statusCode) = await _leaveService.ApplyLeaveAsync(dto, cancellationToken);
        return ToActionResult(result, error, statusCode);
    }

    [HttpPatch("{id:int}/approve")]
    [RequirePermission(HrmsPermissions.LeaveApprove)]
    public async Task<IActionResult> Approve(int id, [FromBody] LeaveActionDto dto, CancellationToken cancellationToken)
    {
        var (result, error, statusCode) = await _leaveService.ApproveLeaveAsync(id, dto, cancellationToken);
        return ToActionResult(result, error, statusCode);
    }

    [HttpPatch("{id:int}/reject")]
    [RequirePermission(HrmsPermissions.LeaveApprove)]
    public async Task<IActionResult> Reject(int id, [FromBody] LeaveActionDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (result, error, statusCode) = await _leaveService.RejectLeaveAsync(id, dto, cancellationToken);
        return ToActionResult(result, error, statusCode);
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var (result, error, statusCode) = await _leaveService.CancelLeaveAsync(id, cancellationToken);
        return ToActionResult(result, error, statusCode);
    }

    [HttpPatch("{id:int}/status")]
    [RequirePermission(HrmsPermissions.LeaveApprove)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] LeaveStatusUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var action = new LeaveActionDto { Remarks = dto.Remarks };

        if (string.Equals(dto.Status, LeaveStatus.Approved, StringComparison.OrdinalIgnoreCase))
        {
            var (result, error, statusCode) = await _leaveService.ApproveLeaveAsync(id, action, cancellationToken);
            return ToActionResult(result, error, statusCode);
        }

        if (string.Equals(dto.Status, LeaveStatus.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            var (result, error, statusCode) = await _leaveService.RejectLeaveAsync(id, action, cancellationToken);
            return ToActionResult(result, error, statusCode);
        }

        return BadRequest("Status must be Approved or Rejected. Use the cancel endpoint for pending cancellations.");
    }

    [HttpPost("{id:int}/attachment")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadAttachment(int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null)
        {
            return BadRequest("Attachment file is required.");
        }

        var (result, error, statusCode) = await _leaveService.SaveAttachmentAsync(id, file, cancellationToken);
        return ToActionResult(result, error, statusCode);
    }

    private IActionResult ToActionResult(LeaveRequestResponseDto? result, string? error, int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status200OK when result != null => Ok(result),
            StatusCodes.Status403Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = error }),
            StatusCodes.Status404NotFound => NotFound(new { message = error }),
            StatusCodes.Status400BadRequest => BadRequest(new { message = error }),
            _ => StatusCode(statusCode, new { message = error ?? "Unexpected error." })
        };
}
