using HRMS.DTOs;
using HRMS.Interfaces;
using HRMS.Models;
using HRMS.Services;

namespace HRMS.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeAccountService _employeeAccountService;

    public EmployeeService(IEmployeeRepository repository, IEmployeeAccountService employeeAccountService)
    {
        _repository = repository;
        _employeeAccountService = employeeAccountService;
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _repository.GetAllAsync(cancellationToken);
        return employees.Select(MapToDto).ToList();
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<(EmployeeDto? Employee, string? Error)> CreateAsync(
        EmployeeUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim();
        if (await _repository.EmailExistsAsync(email, cancellationToken: cancellationToken))
        {
            return (null, "An employee with this email already exists.");
        }

        var employee = MapToEntity(dto);
        employee.CreatedAt = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(employee, cancellationToken);
        var loaded = await _repository.GetByIdAsync(created.Id, cancellationToken);
        var accountEmployee = loaded ?? created;
        await _employeeAccountService.EnsureEmployeeAccountAsync(accountEmployee, cancellationToken);
        return (loaded == null ? MapToDto(created) : MapToDto(loaded), null);
    }

    public async Task<(EmployeeDto? Employee, string? Error)> UpdateAsync(
        int id,
        EmployeeUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return (null, null);
        }

        var email = dto.Email.Trim();
        if (await _repository.EmailExistsAsync(email, id, cancellationToken))
        {
            return (null, "An employee with this email already exists.");
        }

        var employee = MapToEntity(dto);
        employee.Id = id;
        employee.EmployeeCode = string.IsNullOrWhiteSpace(dto.EmployeeCode)
            ? existing.EmployeeCode
            : dto.EmployeeCode.Trim();
        employee.CreatedAt = existing.CreatedAt;
        employee.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(employee, cancellationToken);
        if (updated == null)
        {
            return (null, null);
        }

        var loaded = await _repository.GetByIdAsync(id, cancellationToken);
        if (loaded != null)
        {
            await _employeeAccountService.EnsureEmployeeAccountAsync(loaded, cancellationToken);
        }

        return (loaded == null ? MapToDto(updated) : MapToDto(loaded), null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }

    private static EmployeeDto MapToDto(Employee employee) => new()
    {
        Id = employee.Id,
        EmployeeCode = employee.EmployeeCode,
        FullName = employee.FullName,
        Email = employee.Email,
        PhoneNumber = employee.PhoneNumber,
        DepartmentId = employee.DepartmentId,
        DepartmentName = employee.Department?.Name ?? string.Empty,
        DesignationId = employee.DesignationId,
        DesignationName = employee.Designation?.Name ?? string.Empty,
        BranchId = employee.BranchId,
        BranchName = employee.Branch?.Name ?? string.Empty,
        Status = employee.Status,
        JoiningDate = employee.JoiningDate,
        DateOfBirth = employee.DateOfBirth,
        Gender = employee.Gender,
        BloodGroup = employee.BloodGroup,
        BankName = employee.BankName,
        AccountNumber = employee.AccountNumber,
        IfscCode = employee.IfscCode,
        CreatedAt = employee.CreatedAt,
        UpdatedAt = employee.UpdatedAt
    };

    private static Employee MapToEntity(EmployeeUpsertDto dto)
    {
        var employee = new Employee();
        ApplyDto(employee, dto);
        return employee;
    }

    private static void ApplyDto(Employee employee, EmployeeUpsertDto dto)
    {
        employee.EmployeeCode = string.IsNullOrWhiteSpace(dto.EmployeeCode)
            ? $"EMP-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}"
            : dto.EmployeeCode.Trim();
        employee.FullName = dto.FullName.Trim();
        employee.Email = dto.Email.Trim();
        employee.PhoneNumber = dto.PhoneNumber.Trim();
        employee.DepartmentId = dto.DepartmentId;
        employee.DesignationId = dto.DesignationId;
        employee.BranchId = dto.BranchId;
        employee.Status = dto.Status.Trim();
        employee.JoiningDate = dto.JoiningDate;
        employee.DateOfBirth = dto.DateOfBirth;
        employee.Gender = dto.Gender?.Trim();
        employee.BloodGroup = dto.BloodGroup?.Trim();
        employee.BankName = dto.BankName?.Trim();
        employee.AccountNumber = dto.AccountNumber?.Trim();
        employee.IfscCode = dto.IfscCode?.Trim();
    }
}
