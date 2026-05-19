using CRM.Business.Common;
using CRM.Business.Services;
using CRM.DTO;
using CRM.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

[Route("api/notes")]
[ApiController]
public class NoteController(INoteService noteService) : ControllerBase
{
    [HttpPost("AddNote")]
    public async Task<IActionResult> AddNote([FromQuery] int userId, [FromBody] NoteUpsertDto dto) =>
        (await noteService.AddAsync(userId, dto)).ToActionResult();

    [HttpGet("GetNotes")]
    public async Task<IActionResult> GetNotes([FromQuery] int userId)
    {
        _ = userId;
        return Ok(await noteService.GetAllAsync());
    }

    [HttpGet("GetNotesByRecord/{recordId}")]
    public async Task<IActionResult> GetNotesByRecord(int recordId, [FromQuery] int userId, [FromQuery] string? status = null)
    {
        _ = userId;
        return Ok(await noteService.GetByRecordAsync(recordId, status));
    }

    [HttpGet("GetNotesByAuthor/{authorId}")]
    public async Task<IActionResult> GetNotesByAuthor(int authorId, [FromQuery] int userId)
    {
        _ = userId;
        return Ok(await noteService.GetByAuthorAsync(authorId));
    }

    [HttpPut("UpdateNote/{id}")]
    public async Task<IActionResult> UpdateNote(int id, [FromQuery] int userId, [FromBody] NoteUpsertDto dto) =>
        (await noteService.UpdateAsync(id, userId, dto)).ToActionResult();

    [HttpDelete("DeleteNote/{id}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var result = await noteService.DeleteAsync(id);
        return result.Status == ServiceStatus.Success
            ? Ok("Deleted Successfully")
            : result.ToActionResult();
    }
}
