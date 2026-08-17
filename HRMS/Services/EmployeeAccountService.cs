using HRMS.Data;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public interface IEmployeeAccountService
{
    Task SyncAllEmployeeAccountsAsync(CancellationToken cancellationToken = default);
    Task EnsureEmployeeAccountAsync(Employee employee, CancellationToken cancellationToken = default);
}

public sealed class EmployeeAccountService : IEmployeeAccountService
{
    public const string DefaultEmployeePassword = "Admin@123";
    private const string LegacyPlaceholderEmail = "employee@hrms.com";

    private readonly HRMSDbContext _context;

    public EmployeeAccountService(HRMSDbContext context)
    {
        _context = context;
    }

    public async Task SyncAllEmployeeAccountsAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var employee in employees)
        {
            await EnsureEmployeeAccountAsync(employee, cancellationToken, saveChanges: true);
        }

        var legacyPlaceholder = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == LegacyPlaceholderEmail, cancellationToken);

        if (legacyPlaceholder != null && legacyPlaceholder.EmployeeId == null)
        {
            _context.Users.Remove(legacyPlaceholder);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public Task EnsureEmployeeAccountAsync(Employee employee, CancellationToken cancellationToken = default) =>
        EnsureEmployeeAccountAsync(employee, cancellationToken, saveChanges: true);

    private async Task EnsureEmployeeAccountAsync(
        Employee employee,
        CancellationToken cancellationToken,
        bool saveChanges)
    {
        var email = employee.Email.Trim().ToLowerInvariant();
        var isActive = employee.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultEmployeePassword);
        var employeeRoleId = RoleSeed.Employee.Id;

        var linkedUser = await _context.Users
            .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id, cancellationToken);

        if (linkedUser != null)
        {
            linkedUser.FullName = employee.FullName;
            linkedUser.Email = email;
            linkedUser.RoleId = employeeRoleId;
            linkedUser.IsActive = isActive;
            linkedUser.UpdatedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(linkedUser.PasswordHash))
            {
                linkedUser.PasswordHash = passwordHash;
            }
        }
        else
        {
            var existingByEmail = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

            if (existingByEmail != null)
            {
                if (existingByEmail.RoleId != employeeRoleId)
                {
                    return;
                }

                existingByEmail.EmployeeId = employee.Id;
                existingByEmail.FullName = employee.FullName;
                existingByEmail.IsActive = isActive;
                existingByEmail.UpdatedAt = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(existingByEmail.PasswordHash))
                {
                    existingByEmail.PasswordHash = passwordHash;
                }
            }
            else
            {
                _context.Users.Add(new User
                {
                    FullName = employee.FullName,
                    Email = email,
                    PasswordHash = passwordHash,
                    RoleId = employeeRoleId,
                    EmployeeId = employee.Id,
                    IsActive = isActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        if (saveChanges)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
