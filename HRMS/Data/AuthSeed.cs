using HRMS.Models;
using HRMS.Services;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Data;

public static class AuthSeed
{
    public static async Task SeedAsync(HRMSDbContext context, IEmployeeAccountService employeeAccountService)
    {
        await RoleSeed.SeedAsync(context);
        await SeedAdminUsersAsync(context);
        await ResetUserIdentitySequenceAsync(context);
        await employeeAccountService.SyncAllEmployeeAccountsAsync();
    }

    private static async Task SeedAdminUsersAsync(HRMSDbContext context)
    {
        var now = DateTime.UtcNow;
        var added = false;

        if (!await context.Users.AnyAsync(x => x.RoleId == RoleSeed.SuperAdmin.Id))
        {
            context.Users.Add(new User
            {
                FullName = "Super Admin",
                Email = "superadmin@hrms.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = RoleSeed.SuperAdmin.Id,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            added = true;
        }

        if (!await context.Users.AnyAsync(x => x.RoleId == RoleSeed.HrAdmin.Id))
        {
            context.Users.Add(new User
            {
                FullName = "HR Admin",
                Email = "hradmin@hrms.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = RoleSeed.HrAdmin.Id,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            added = true;
        }

        if (added)
        {
            await context.SaveChangesAsync();
        }
    }

    private static Task ResetUserIdentitySequenceAsync(HRMSDbContext context) =>
        context.Database.ExecuteSqlRawAsync(
            "SELECT setval(pg_get_serial_sequence('users', 'id'), COALESCE((SELECT MAX(id) FROM users), 0), true)");
}
