using CRM.DATA;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoteController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public NoteController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpPost("AddNote")]
        public async Task<IActionResult> AddNote(Note note)
        {
            if (note == null)
            {
                return BadRequest();
            }

            var now = DateTime.UtcNow;
            note.Id = 0;
            note.CreatedAt = now;
            note.UpdatedAt = now;

            await _context.Notes.AddAsync(note);
            await _context.SaveChangesAsync();

            return Ok(note);
        }

        [HttpGet("GetNotes")]
        public async Task<IActionResult> GetNotes()
        {
            var data = await _context.Notes
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("GetNotesByRecord/{recordId}")]
        public async Task<IActionResult> GetNotesByRecord(int recordId, [FromQuery] string? status = null)
        {
            var query = _context.Notes.AsNoTracking().Where(n => n.RecordId == recordId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(n => n.Status == status);
            }

            var data = await query.OrderByDescending(n => n.UpdatedAt).ToListAsync();
            return Ok(data);
        }

        [HttpGet("GetNotesByAuthor/{authorId}")]
        public async Task<IActionResult> GetNotesByAuthor(int authorId)
        {
            var data = await _context.Notes
                .AsNoTracking()
                .Where(n => n.AuthorId == authorId)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            return Ok(data);
        }

        [HttpPut("UpdateNote/{id}")]
        public async Task<IActionResult> UpdateNote(int id, Note updated)
        {
            if (id != updated.Id)
            {
                return BadRequest();
            }

            var existing = await _context.Notes.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.RecordId = updated.RecordId;
            existing.AuthorId = updated.AuthorId;
            existing.Name = updated.Name;
            existing.Title = updated.Title;
            existing.NoteText = updated.NoteText;
            existing.Status = updated.Status;
            existing.Priority = updated.Priority;
            existing.Tags = updated.Tags;
            existing.Attachments = updated.Attachments;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("DeleteNote/{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                return NotFound();
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return Ok("Deleted Successfully");
        }
    }
}
