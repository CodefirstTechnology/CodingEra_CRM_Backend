using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly HRMSDbContext _context;

    public EmployeeRepository(HRMSDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Branch)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Branch)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var query = _context.Employees.Where(e => e.Email.ToLower() == normalizedEmail);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public async Task<Employee?> UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employee.Id, cancellationToken);

        if (existing == null)
        {
            return null;
        }

        existing.EmployeeCode = employee.EmployeeCode;
        existing.FullName = employee.FullName;
        existing.Email = employee.Email;
        existing.PhoneNumber = employee.PhoneNumber;
        existing.DepartmentId = employee.DepartmentId;
        existing.DesignationId = employee.DesignationId;
        existing.BranchId = employee.BranchId;
        existing.Status = employee.Status;
        existing.JoiningDate = employee.JoiningDate;
        existing.DateOfBirth = employee.DateOfBirth;
        existing.Gender = employee.Gender;
        existing.BloodGroup = employee.BloodGroup;
        existing.BankName = employee.BankName;
        existing.AccountNumber = employee.AccountNumber;
        existing.IfscCode = employee.IfscCode;
        existing.UpdatedAt = employee.UpdatedAt;

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (existing == null)
        {
            return false;
        }

        _context.Employees.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
