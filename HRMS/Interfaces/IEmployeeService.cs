using HRMS.DTOs;

namespace HRMS.Interfaces;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(EmployeeDto? Employee, string? Error)> CreateAsync(EmployeeUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(EmployeeDto? Employee, string? Error)> UpdateAsync(int id, EmployeeUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
