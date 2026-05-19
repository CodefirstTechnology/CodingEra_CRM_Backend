using CRM.Business.Services;
using CRM.DTO;
using CRM.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

[Route("api/deals")]
[ApiController]
public class DealsController(IDealService dealService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int userId, [FromQuery] string? status = null)
    {
        _ = userId;
        return Ok(await dealService.GetAllAsync(status));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
    {
        _ = userId;
        var deal = await dealService.GetByIdAsync(id);
        return deal == null ? NotFound() : Ok(deal);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] DealUpsertDto dto) =>
        (await dealService.CreateAsync(userId, dto)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] DealUpsertDto dto) =>
        (await dealService.UpdateAsync(id, userId, dto)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        (await dealService.DeleteAsync(id)).ToActionResult(new { deleted = true });
}
