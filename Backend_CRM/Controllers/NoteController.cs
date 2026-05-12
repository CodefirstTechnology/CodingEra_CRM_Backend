using CRM.DATA;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/notes")]
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
            SyncNoteRecordId(note);

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

            existing.AuthorId = updated.AuthorId;
            existing.Name = updated.Name;
            existing.Title = updated.Title;
            existing.NoteText = updated.NoteText;
            existing.RelatedType = updated.RelatedType;
            existing.RelatedEntityId = updated.RelatedEntityId;
            existing.RelatedName = updated.RelatedName;
            existing.Visibility = updated.Visibility;
            existing.RelatedLeadId = updated.RelatedLeadId;
            existing.RelatedDealId = updated.RelatedDealId;
            existing.RelatedContactId = updated.RelatedContactId;
            existing.RelatedOrganizationId = updated.RelatedOrganizationId;
            existing.RecordId = updated.RecordId;
            SyncNoteRecordId(existing);
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

        /// <summary>Keeps legacy <see cref="Note.RecordId"/> in sync with typed FKs for lead/deal.</summary>
        private static void SyncNoteRecordId(Note note)
        {
            if (note.RelatedLeadId is > 0)
            {
                note.RecordId = note.RelatedLeadId.Value;
            }
            else if (note.RelatedDealId is > 0)
            {
                note.RecordId = note.RelatedDealId.Value;
            }
            else if (note.RelatedEntityId is > 0)
            {
                note.RecordId = note.RelatedEntityId.Value;
            }
        }
    }
}
