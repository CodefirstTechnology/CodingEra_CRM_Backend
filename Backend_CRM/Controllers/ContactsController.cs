using CRM.Business.Services;
using CRM.DTO;
using CRM.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

[Route("api/contacts")]
[ApiController]
public class ContactsController(IContactService contactService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int userId, [FromQuery] int? organizationId = null)
    {
        _ = userId;
        return Ok(await contactService.GetAllAsync(organizationId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
    {
        _ = userId;
        var contact = await contactService.GetByIdAsync(id);
        return contact == null ? NotFound() : Ok(contact);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] ContactUpsertDto dto) =>
        (await contactService.CreateAsync(userId, dto)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] ContactUpsertDto dto) =>
        (await contactService.UpdateAsync(id, userId, dto)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        (await contactService.DeleteAsync(id)).ToActionResult(new { deleted = true });
}
