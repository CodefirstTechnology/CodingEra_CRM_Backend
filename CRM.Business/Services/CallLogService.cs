using CRM.Business.Common;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services;

public sealed class CallLogService(TaskDbcontext db, IAuditUserService auditUserService) : ICallLogService
{
    public async Task<ServiceResult<CallLog>> AddAsync(int userId, CallLogUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<CallLog>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<CallLog>.Fail(auditResult.Status, auditResult.Message);
        }

        var call = CrmWriteMappings.ToCallLog(dto, 0);
        call.CallId = 0;
        await db.CallLogs.AddAsync(call);
        await db.SaveChangesAsync();
        return ServiceResult<CallLog>.Ok(call);
    }

    public async Task<IReadOnlyList<CallLog>> GetAllAsync() =>
        await db.CallLogs.ToListAsync();

    public async Task<ServiceResult<CallLog>> UpdateAsync(int id, int userId, CallLogUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<CallLog>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<CallLog>.Fail(auditResult.Status, auditResult.Message);
        }

        if (dto.CallId != 0 && dto.CallId != id)
        {
            return ServiceResult<CallLog>.Fail(
                ServiceStatus.BadRequest,
                "Route id and body callId must match when the body includes a call id.");
        }

        var existingCall = await db.CallLogs.FindAsync(id);
        if (existingCall == null)
        {
            return ServiceResult<CallLog>.Fail(ServiceStatus.NotFound);
        }

        CrmWriteMappings.Apply(existingCall, dto);
        await db.SaveChangesAsync();
        return ServiceResult<CallLog>.Ok(existingCall);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var call = await db.CallLogs.FindAsync(id);
        if (call == null)
        {
            return ServiceResult.Fail(ServiceStatus.NotFound);
        }

        db.CallLogs.Remove(call);
        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
