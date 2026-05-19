using CRM.Business.Services;
using CRM.DTO;
using CRM.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

[Route("api/leads")]
[ApiController]
public class LeadsController(ILeadService leadService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int userId,
        [FromQuery] string? leadSource = null,
        [FromQuery] string? status = null)
    {
        _ = userId;
        return Ok(await leadService.GetAllAsync(leadSource, status));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
    {
        _ = userId;
        var lead = await leadService.GetByIdAsync(id);
        return lead == null ? NotFound() : Ok(lead);
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id, [FromQuery] int userId)
    {
        _ = userId;
        return (await leadService.GetHistoryAsync(id)).ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] LeadUpsertDto dto) =>
        (await leadService.CreateAsync(userId, dto)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] LeadUpsertDto dto) =>
        (await leadService.UpdateAsync(id, userId, dto)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        (await leadService.DeleteAsync(id)).ToActionResult(new { deleted = true });
}
