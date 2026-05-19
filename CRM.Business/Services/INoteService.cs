using CRM.Business.Common;
using CRM.DTO;
using CRM.models;

namespace CRM.Business.Services;

public interface INoteService
{
    Task<ServiceResult<Note>> AddAsync(int userId, NoteUpsertDto dto);
    Task<IReadOnlyList<Note>> GetAllAsync();
    Task<IReadOnlyList<Note>> GetByRecordAsync(int recordId, string? status);
    Task<IReadOnlyList<Note>> GetByAuthorAsync(int authorId);
    Task<ServiceResult<Note>> UpdateAsync(int id, int userId, NoteUpsertDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}
