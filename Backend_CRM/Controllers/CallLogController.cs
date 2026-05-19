using CRM.Business.Common;
using CRM.Business.Services;
using CRM.DTO;
using CRM.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

[Route("api/callLogs")]
[ApiController]
public class CallLogController(ICallLogService callLogService) : ControllerBase
{
    [HttpPost("AddCall")]
    public async Task<IActionResult> AddCall([FromQuery] int userId, [FromBody] CallLogUpsertDto dto) =>
        (await callLogService.AddAsync(userId, dto)).ToActionResult();

    [HttpGet("GetCalls")]
    public async Task<IActionResult> GetCalls([FromQuery] int userId)
    {
        _ = userId;
        return Ok(await callLogService.GetAllAsync());
    }

    [HttpPut("UpdateCall/{id}")]
    public async Task<IActionResult> UpdateCall(int id, [FromQuery] int userId, [FromBody] CallLogUpsertDto dto) =>
        (await callLogService.UpdateAsync(id, userId, dto)).ToActionResult();

    [HttpDelete("DeleteCall/{id}")]
    public async Task<IActionResult> DeleteCall(int id)
    {
        var result = await callLogService.DeleteAsync(id);
        return result.Status == ServiceStatus.Success
            ? Ok("Deleted Successfully")
            : result.ToActionResult();
    }
}
