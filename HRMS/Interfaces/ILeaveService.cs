using HRMS.DTOs;

namespace HRMS.Interfaces;

public interface ILeaveService
{
    Task<IReadOnlyList<LeaveRequestResponseDto>> GetRequestsAsync(
        int? employeeId,
        int? departmentId,
        int? leaveTypeId,
        string? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestResponseDto?> GetRequestByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> ApplyLeaveAsync(
        LeaveApplyDto dto,
        CancellationToken cancellationToken = default);

    Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> ApproveLeaveAsync(
        int id,
        LeaveActionDto dto,
        CancellationToken cancellationToken = default);

    Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> RejectLeaveAsync(
        int id,
        LeaveActionDto dto,
        CancellationToken cancellationToken = default);

    Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> CancelLeaveAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveBalanceItemDto>> GetBalancesAsync(
        int? employeeId,
        int? year,
        CancellationToken cancellationToken = default);

    Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> SaveAttachmentAsync(
        int id,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveNotificationDto>> GetNotificationsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> MarkNotificationReadAsync(int id, CancellationToken cancellationToken = default);
}
