using CRM.Business.Common;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services;

public sealed class NoteService(TaskDbcontext db, IAuditUserService auditUserService) : INoteService
{
    public async Task<ServiceResult<Note>> AddAsync(int userId, NoteUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Note>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Note>.Fail(auditResult.Status, auditResult.Message);
        }

        var note = CrmWriteMappings.ToNote(dto, 0);
        note.Id = 0;
        SyncNoteRecordId(note);
        await db.Notes.AddAsync(note);
        await db.SaveChangesAsync();
        return ServiceResult<Note>.Ok(note);
    }

    public async Task<IReadOnlyList<Note>> GetAllAsync() =>
        await db.Notes.OrderByDescending(n => n.UpdatedAt).ToListAsync();

    public async Task<IReadOnlyList<Note>> GetByRecordAsync(int recordId, string? status)
    {
        var query = db.Notes.AsNoTracking().Where(n => n.RecordId == recordId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(n => n.Status == status);
        }

        return await query.OrderByDescending(n => n.UpdatedAt).ToListAsync();
    }

    public async Task<IReadOnlyList<Note>> GetByAuthorAsync(int authorId) =>
        await db.Notes
            .AsNoTracking()
            .Where(n => n.AuthorId == authorId)
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync();

    public async Task<ServiceResult<Note>> UpdateAsync(int id, int userId, NoteUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Note>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Note>.Fail(auditResult.Status, auditResult.Message);
        }

        if (dto.Id != 0 && dto.Id != id)
        {
            return ServiceResult<Note>.Fail(
                ServiceStatus.BadRequest,
                "Route id and body id must match when the body includes an id.");
        }

        var existing = await db.Notes.FindAsync(id);
        if (existing == null)
        {
            return ServiceResult<Note>.Fail(ServiceStatus.NotFound);
        }

        CrmWriteMappings.Apply(existing, dto);
        SyncNoteRecordId(existing);
        await db.SaveChangesAsync();
        return ServiceResult<Note>.Ok(existing);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var note = await db.Notes.FindAsync(id);
        if (note == null)
        {
            return ServiceResult.Fail(ServiceStatus.NotFound);
        }

        db.Notes.Remove(note);
        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

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
