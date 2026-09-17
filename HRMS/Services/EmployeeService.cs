using HRMS.Data;
using HRMS.DTOs;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeAccountService _employeeAccountService;
    private readonly HRMSDbContext _context;

    public EmployeeService(
        IEmployeeRepository repository,
        IEmployeeAccountService employeeAccountService,
        HRMSDbContext context)
    {
        _repository = repository;
        _employeeAccountService = employeeAccountService;
        _context = context;
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

        // Validate branch & department
        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == dto.BranchId, cancellationToken);
        if (branch == null || !branch.IsActive)
        {
            return (null, "Selected branch is invalid or inactive.");
        }

        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == dto.DepartmentId, cancellationToken);
        if (department == null || !department.IsActive)
        {
            return (null, "Selected department is invalid or inactive.");
        }

        // Validate manager
        if (dto.ReportingManagerId.HasValue)
        {
            var manager = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.ReportingManagerId.Value, cancellationToken);
            if (manager == null)
            {
                return (null, "Selected reporting manager does not exist.");
            }
        }

        var employee = MapToEntity(dto);
        employee.CreatedAt = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(employee, cancellationToken);

        // Record Lifecycle History
        _context.EmployeeLifecycleHistories.Add(new EmployeeLifecycleHistory
        {
            EmployeeId = created.Id,
            EventType = "Employee Created",
            OldValue = null,
            NewValue = $"Joined as {dto.FullName} ({dto.EmploymentType})",
            Reason = "Initial onboarding / creation",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

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

        // Validate branch & department
        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == dto.BranchId, cancellationToken);
        if (branch == null || !branch.IsActive)
        {
            return (null, "Selected branch is invalid or inactive.");
        }

        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == dto.DepartmentId, cancellationToken);
        if (department == null || !department.IsActive)
        {
            return (null, "Selected department is invalid or inactive.");
        }

        // Validate circular reporting hierarchy
        if (dto.ReportingManagerId.HasValue)
        {
            if (dto.ReportingManagerId.Value == id)
            {
                return (null, "An employee cannot be their own reporting manager.");
            }

            var visited = new HashSet<int> { id };
            int? currentMgrId = dto.ReportingManagerId.Value;

            while (currentMgrId.HasValue)
            {
                if (visited.Contains(currentMgrId.Value))
                {
                    return (null, "Circular reporting hierarchy detected. Please select a valid reporting manager.");
                }

                visited.Add(currentMgrId.Value);

                var mgr = await _context.Employees
                    .AsNoTracking()
                    .Where(e => e.Id == currentMgrId.Value)
                    .Select(e => new { e.ReportingManagerId })
                    .FirstOrDefaultAsync(cancellationToken);

                currentMgrId = mgr?.ReportingManagerId;
            }
        }

        // Check for major changes to record in history
        var historyEvents = new List<EmployeeLifecycleHistory>();
        if (existing.DepartmentId != dto.DepartmentId)
        {
            var oldDept = existing.Department?.Name ?? existing.DepartmentId.ToString();
            var newDept = department.Name;
            historyEvents.Add(new EmployeeLifecycleHistory
            {
                EmployeeId = id,
                EventType = "Department Changed",
                OldValue = oldDept,
                NewValue = newDept,
                Reason = "Profile update",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (existing.BranchId != dto.BranchId)
        {
            var oldBranch = existing.Branch?.Name ?? existing.BranchId.ToString();
            var newBranch = branch.Name;
            historyEvents.Add(new EmployeeLifecycleHistory
            {
                EmployeeId = id,
                EventType = "Branch Changed",
                OldValue = oldBranch,
                NewValue = newBranch,
                Reason = "Profile update",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (existing.DesignationId != dto.DesignationId)
        {
            var oldDesig = existing.Designation?.Name ?? existing.DesignationId.ToString();
            var newDesig = await _context.Designations.Where(d => d.Id == dto.DesignationId).Select(d => d.Name).FirstOrDefaultAsync(cancellationToken);
            historyEvents.Add(new EmployeeLifecycleHistory
            {
                EmployeeId = id,
                EventType = "Designation Changed",
                OldValue = oldDesig,
                NewValue = newDesig,
                Reason = "Profile update",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (existing.ReportingManagerId != dto.ReportingManagerId)
        {
            var oldMgr = existing.ReportingManager?.FullName ?? "None";
            var newMgr = dto.ReportingManagerId.HasValue
                ? await _context.Employees.Where(e => e.Id == dto.ReportingManagerId.Value).Select(e => e.FullName).FirstOrDefaultAsync(cancellationToken)
                : "None";
            historyEvents.Add(new EmployeeLifecycleHistory
            {
                EmployeeId = id,
                EventType = "Manager Changed",
                OldValue = oldMgr,
                NewValue = newMgr,
                Reason = "Profile update",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!string.Equals(existing.Status, dto.Status, StringComparison.OrdinalIgnoreCase))
        {
            historyEvents.Add(new EmployeeLifecycleHistory
            {
                EmployeeId = id,
                EventType = "Status Changed",
                OldValue = existing.Status,
                NewValue = dto.Status,
                Reason = "Profile status update",
                CreatedAt = DateTime.UtcNow
            });
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

        if (historyEvents.Count > 0)
        {
            _context.EmployeeLifecycleHistories.AddRange(historyEvents);
            await _context.SaveChangesAsync(cancellationToken);
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
        FirstName = employee.FirstName,
        MiddleName = employee.MiddleName,
        LastName = employee.LastName,
        FullName = employee.FullName,
        Email = employee.Email,
        WorkEmail = employee.WorkEmail,
        PhoneNumber = employee.PhoneNumber,
        AlternatePhone = employee.AlternatePhone,
        DepartmentId = employee.DepartmentId,
        DepartmentName = employee.Department?.Name ?? string.Empty,
        DesignationId = employee.DesignationId,
        DesignationName = employee.Designation?.Name ?? string.Empty,
        BranchId = employee.BranchId,
        BranchName = employee.Branch?.Name ?? string.Empty,
        GradeId = employee.GradeId,
        GradeName = employee.Grade?.GradeName,
        CostCenterId = employee.CostCenterId,
        CostCenterName = employee.CostCenter?.CostCenterName,
        ShiftId = employee.ShiftId,
        ShiftName = employee.Shift?.ShiftName,
        ReportingManagerId = employee.ReportingManagerId,
        ReportingManagerName = employee.ReportingManager?.FullName,
        WorkLocation = employee.WorkLocation,
        EmploymentType = employee.EmploymentType,
        Status = employee.Status,
        JoiningDate = employee.JoiningDate,
        DateOfBirth = employee.DateOfBirth,
        Gender = employee.Gender,
        BloodGroup = employee.BloodGroup,
        MaritalStatus = employee.MaritalStatus,
        ProfilePhotoUrl = employee.ProfilePhotoUrl,
        CurrentAddress = employee.CurrentAddress,
        PermanentAddress = employee.PermanentAddress,
        EmergencyContactName = employee.EmergencyContactName,
        EmergencyContactPhone = employee.EmergencyContactPhone,
        Pan = employee.Pan,
        Aadhaar = employee.Aadhaar,
        PassportNumber = employee.PassportNumber,
        DrivingLicense = employee.DrivingLicense,
        Uan = employee.Uan,
        EsicNumber = employee.EsicNumber,
        BankName = employee.BankName,
        AccountNumber = employee.AccountNumber,
        IfscCode = employee.IfscCode,
        AccountHolderName = employee.AccountHolderName,
        CurrentCtc = employee.CurrentCtc,
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

        var fullName = !string.IsNullOrWhiteSpace(dto.FullName)
            ? dto.FullName.Trim()
            : $"{dto.FirstName} {dto.LastName}".Trim();

        employee.FirstName = dto.FirstName?.Trim();
        employee.MiddleName = dto.MiddleName?.Trim();
        employee.LastName = dto.LastName?.Trim();
        employee.FullName = string.IsNullOrWhiteSpace(fullName) ? "New Employee" : fullName;

        employee.Email = dto.Email.Trim();
        employee.WorkEmail = dto.WorkEmail?.Trim();
        employee.PhoneNumber = dto.PhoneNumber.Trim();
        employee.AlternatePhone = dto.AlternatePhone?.Trim();

        employee.DepartmentId = dto.DepartmentId;
        employee.DesignationId = dto.DesignationId;
        employee.BranchId = dto.BranchId;
        employee.GradeId = dto.GradeId;
        employee.CostCenterId = dto.CostCenterId;
        employee.ShiftId = dto.ShiftId;
        employee.ReportingManagerId = dto.ReportingManagerId;
        employee.WorkLocation = dto.WorkLocation?.Trim();
        employee.EmploymentType = string.IsNullOrWhiteSpace(dto.EmploymentType) ? "Full Time" : dto.EmploymentType.Trim();
        employee.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim();

        employee.JoiningDate = dto.JoiningDate;
        employee.DateOfBirth = dto.DateOfBirth;
        employee.Gender = dto.Gender?.Trim();
        employee.BloodGroup = dto.BloodGroup?.Trim();
        employee.MaritalStatus = dto.MaritalStatus?.Trim();
        employee.ProfilePhotoUrl = dto.ProfilePhotoUrl?.Trim();

        employee.CurrentAddress = dto.CurrentAddress?.Trim();
        employee.PermanentAddress = dto.PermanentAddress?.Trim();
        employee.EmergencyContactName = dto.EmergencyContactName?.Trim();
        employee.EmergencyContactPhone = dto.EmergencyContactPhone?.Trim();

        employee.Pan = dto.Pan?.Trim();
        employee.Aadhaar = dto.Aadhaar?.Trim();
        employee.PassportNumber = dto.PassportNumber?.Trim();
        employee.DrivingLicense = dto.DrivingLicense?.Trim();
        employee.Uan = dto.Uan?.Trim();
        employee.EsicNumber = dto.EsicNumber?.Trim();

        employee.BankName = dto.BankName?.Trim();
        employee.AccountNumber = dto.AccountNumber?.Trim();
        employee.IfscCode = dto.IfscCode?.Trim();
        employee.AccountHolderName = dto.AccountHolderName?.Trim();
        employee.CurrentCtc = dto.CurrentCtc;
    }
}
