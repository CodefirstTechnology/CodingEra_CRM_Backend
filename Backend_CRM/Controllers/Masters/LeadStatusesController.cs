using CRM.Business.Services.MasterData;
using CRM.DTO;
using CRM.Extensions;
using CRM.models;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers.Masters;

[Route("api/MasterData/lead-statuses")]
[ApiController]
public class LeadStatusesController(IMasterDataService<LeadStatus> service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int userId, [FromQuery] bool activeOnly = false)
    {
        _ = userId;
        return Ok(await service.GetAllAsync(activeOnly));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
    {
        _ = userId;
        var item = await service.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] MasterDataUpsertDto dto) =>
        (await service.CreateAsync(userId, dto)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] MasterDataUpsertDto dto) =>
        (await service.UpdateAsync(id, userId, dto)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        (await service.DeleteAsync(id)).ToActionResult(new { deleted = true });
}
