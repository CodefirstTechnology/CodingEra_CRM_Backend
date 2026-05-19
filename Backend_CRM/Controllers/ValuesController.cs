using CRM.Business.Services;
using CRM.DTO;
using CRM.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ValuesController(ITaskService taskService) : ControllerBase
{
    [HttpPost("Addtask")]
    public async Task<IActionResult> AddTask([FromQuery] int userId, [FromBody] TaskUpsertDto dto) =>
        (await taskService.CreateAsync(userId, dto)).ToActionResult();

    [HttpGet("Gettask")]
    public async Task<IActionResult> Gettask([FromQuery] int userId)
    {
        _ = userId;
        return Ok(await taskService.GetAllAsync(null, null));
    }

    [HttpDelete("Deletetask/{id}")]
    public async Task<IActionResult> Deletetask(int id) =>
        (await taskService.DeleteAsync(id)).ToActionResult("Task deleted successfully");

    [HttpPut("Updatetask/{id}")]
    public async Task<IActionResult> Updatetask(int id, [FromQuery] int userId, [FromBody] TaskUpsertDto dto) =>
        (await taskService.UpdateAsync(id, userId, dto)).ToActionResult();
}
