using CRM.Business.Services;
using CRM.DTO;
using CRM.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

[Route("api/organizations")]
[ApiController]
public class OrganizationsController(IOrganizationService organizationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int userId)
    {
        _ = userId;
        return Ok(await organizationService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
    {
        _ = userId;
        var organization = await organizationService.GetByIdAsync(id);
        return organization == null ? NotFound() : Ok(organization);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] OrganizationUpsertDto dto) =>
        (await organizationService.CreateAsync(userId, dto)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] OrganizationUpsertDto dto) =>
        (await organizationService.UpdateAsync(id, userId, dto)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        (await organizationService.DeleteAsync(id)).ToActionResult(new { deleted = true });
}
