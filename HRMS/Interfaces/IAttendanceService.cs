using HRMS.DTOs;

namespace HRMS.Interfaces;

public interface IAttendanceService
{
    Task<(AttendanceResponseDto? Result, string? Error, int StatusCode)> ClockInAsync(
        AttendanceClockDto dto,
        CancellationToken cancellationToken = default);

    Task<(AttendanceResponseDto? Result, string? Error, int StatusCode)> ClockOutAsync(
        AttendanceClockDto dto,
        CancellationToken cancellationToken = default);

    Task<(AttendanceResponseDto? Result, string? Error, int StatusCode)> GetTodayAsync(
        int? employeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceResponseDto>> GetRecordsAsync(
        AttendanceQueryDto query,
        CancellationToken cancellationToken = default);

    Task<AttendanceResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<AttendanceSummaryDto> GetSummaryAsync(
        AttendanceQueryDto query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCsvAsync(
        AttendanceQueryDto query,
        CancellationToken cancellationToken = default);
}
