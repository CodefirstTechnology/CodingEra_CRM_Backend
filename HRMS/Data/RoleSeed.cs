using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Data;

public static class RoleSeed
{
    public static readonly Role SuperAdmin = new()
    {
        Id = 1,
        Name = "Super Admin",
        Code = nameof(UserRole.SUPER_ADMIN),
        Description = "Full system access including HR admin and system settings.",
        IsActive = true
    };

    public static readonly Role HrAdmin = new()
    {
        Id = 2,
        Name = "HR Admin",
        Code = nameof(UserRole.HR_ADMIN),
        Description = "HR operations: employees, payroll, leave approval, reports.",
        IsActive = true
    };

    public static readonly Role Employee = new()
    {
        Id = 3,
        Name = "Employee",
        Code = nameof(UserRole.EMPLOYEE),
        Description = "Self-service access to own profile, attendance, leave, and payslips.",
        IsActive = true
    };

    public static async Task SeedAsync(HRMSDbContext context)
    {
        if (await context.Roles.AnyAsync())
        {
            return;
        }

        context.Roles.AddRange(SuperAdmin, HrAdmin, Employee);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync(
            "SELECT setval(pg_get_serial_sequence('roles', 'id'), COALESCE((SELECT MAX(id) FROM roles), 0), true)");
    }

    public static async Task<int> GetRoleIdAsync(HRMSDbContext context, UserRole role, CancellationToken cancellationToken = default)
    {
        var code = role.ToString();
        var roleId = await context.Roles
            .AsNoTracking()
            .Where(x => x.Code == code)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (roleId == 0)
        {
            throw new InvalidOperationException($"Role '{code}' was not found. Run role seed first.");
        }

        return roleId;
    }
}
