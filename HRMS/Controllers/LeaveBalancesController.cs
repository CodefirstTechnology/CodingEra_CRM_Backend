using HRMS.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Route("api/leave-balances")]
[ApiController]
public class LeaveBalancesController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveBalancesController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBalances(
        [FromQuery] int? employeeId,
        [FromQuery] int? year,
        CancellationToken cancellationToken = default)
    {
        var balances = await _leaveService.GetBalancesAsync(employeeId, year, cancellationToken);
        return Ok(balances);
    }
}
