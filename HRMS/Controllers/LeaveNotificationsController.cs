using HRMS.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Route("api/leave-notifications")]
[ApiController]
public class LeaveNotificationsController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveNotificationsController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var items = await _leaveService.GetNotificationsAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken = default)
    {
        var updated = await _leaveService.MarkNotificationReadAsync(id, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}
