using CRM.DATA;
using CRM.DTO;
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
        public async Task<IActionResult> AddNote([FromBody] NoteUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var note = CrmWriteMappings.ToNote(dto, 0);
            note.Id = 0;
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
        public async Task<IActionResult> UpdateNote(int id, [FromBody] NoteUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (dto.Id != 0 && dto.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var existing = await _context.Notes.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            CrmWriteMappings.Apply(existing, dto);
            SyncNoteRecordId(existing);

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
