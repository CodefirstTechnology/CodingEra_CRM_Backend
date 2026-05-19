using CRM.Business.Services;
using CRM.DTO;
using CRM.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

[Route("api/tasks")]
[ApiController]
public class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int userId,
        [FromQuery] int? relatedLeadId = null,
        [FromQuery] int? relatedDealId = null)
    {
        _ = userId;
        return Ok(await taskService.GetAllAsync(relatedLeadId, relatedDealId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
    {
        _ = userId;
        var task = await taskService.GetByIdAsync(id);
        return task == null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] TaskUpsertDto dto) =>
        (await taskService.CreateAsync(userId, dto)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] TaskUpsertDto dto) =>
        (await taskService.UpdateAsync(id, userId, dto)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        (await taskService.DeleteAsync(id)).ToActionResult(new { deleted = true });
}
