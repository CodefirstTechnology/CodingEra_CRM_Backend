using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Data;

public static class MultiTenantBootstrap
{
    public static async Task EnsureAsync(HRMSDbContext db)
    {
        // 1. Create tables if not exist
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS tenants (
                id SERIAL PRIMARY KEY,
                name VARCHAR(256) NOT NULL,
                code VARCHAR(64) NOT NULL UNIQUE,
                domain VARCHAR(128),
                contact_email VARCHAR(256) NOT NULL,
                contact_phone VARCHAR(32),
                status VARCHAR(32) NOT NULL DEFAULT 'Active',
                plan VARCHAR(32) NOT NULL DEFAULT 'Enterprise',
                max_employees INT NOT NULL DEFAULT 500,
                subscription_expires_at TIMESTAMPTZ,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS audit_logs (
                id SERIAL PRIMARY KEY,
                tenant_id INT,
                user_id INT,
                user_email VARCHAR(256),
                user_name VARCHAR(256),
                user_role VARCHAR(64),
                action VARCHAR(128) NOT NULL,
                entity_name VARCHAR(128) NOT NULL,
                entity_id VARCHAR(64),
                details TEXT,
                ip_address VARCHAR(64),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS company_assets (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                branch_id INT,
                asset_code VARCHAR(64) NOT NULL,
                name VARCHAR(256) NOT NULL,
                category VARCHAR(64) NOT NULL DEFAULT 'Laptop',
                serial_number VARCHAR(128),
                model_number VARCHAR(128),
                status VARCHAR(32) NOT NULL DEFAULT 'Available',
                purchase_date DATE,
                assigned_to_employee_id INT,
                assigned_at TIMESTAMPTZ,
                notes VARCHAR(512),
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        // 2. Add tenant_id and audit columns to all operational tables if missing
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE users ADD COLUMN IF NOT EXISTS tenant_id INT;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS status VARCHAR(32) DEFAULT 'Active';
            ALTER TABLE users ADD COLUMN IF NOT EXISTS last_login_at TIMESTAMPTZ;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS last_login_ip VARCHAR(64);
            ALTER TABLE users ADD COLUMN IF NOT EXISTS password_reset_required BOOLEAN DEFAULT FALSE;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS updated_by INT;

            ALTER TABLE employees ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS updated_by INT;

            ALTER TABLE branches ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS code VARCHAR(32);
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS address VARCHAR(256);
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS updated_by INT;
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ DEFAULT NOW();
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();

            ALTER TABLE departments ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE departments ADD COLUMN IF NOT EXISTS branch_id INT;
            ALTER TABLE departments ADD COLUMN IF NOT EXISTS code VARCHAR(32);
            ALTER TABLE departments ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE departments ADD COLUMN IF NOT EXISTS updated_by INT;
            ALTER TABLE departments ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ DEFAULT NOW();
            ALTER TABLE departments ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();

            ALTER TABLE designations ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE designations ADD COLUMN IF NOT EXISTS department_id INT;
            ALTER TABLE designations ADD COLUMN IF NOT EXISTS code VARCHAR(32);
            ALTER TABLE designations ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE designations ADD COLUMN IF NOT EXISTS updated_by INT;
            ALTER TABLE designations ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ DEFAULT NOW();
            ALTER TABLE designations ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();

            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS updated_by INT;

            ALTER TABLE leave_types ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE leave_types ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE leave_types ADD COLUMN IF NOT EXISTS updated_by INT;
            ALTER TABLE leave_types ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ DEFAULT NOW();
            ALTER TABLE leave_types ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();

            ALTER TABLE leave_allocations ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE leave_allocations ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE leave_allocations ADD COLUMN IF NOT EXISTS updated_by INT;
            ALTER TABLE leave_allocations ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ DEFAULT NOW();
            ALTER TABLE leave_allocations ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();

            ALTER TABLE leave_requests ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE leave_requests ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE leave_requests ADD COLUMN IF NOT EXISTS updated_by INT;

            ALTER TABLE leave_notifications ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE leave_notifications ADD COLUMN IF NOT EXISTS created_by INT;

            ALTER TABLE leave_rejection_reasons ADD COLUMN IF NOT EXISTS tenant_id INT;

            ALTER TABLE document_categories ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE document_categories ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE document_categories ADD COLUMN IF NOT EXISTS updated_by INT;
            ALTER TABLE document_categories ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ DEFAULT NOW();
            ALTER TABLE document_categories ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();

            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS created_by INT;

            ALTER TABLE payroll_records ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE payroll_records ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE payroll_records ADD COLUMN IF NOT EXISTS updated_by INT;
            ALTER TABLE payroll_records ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();

            ALTER TABLE performance_reviews ADD COLUMN IF NOT EXISTS tenant_id INT DEFAULT 1;
            ALTER TABLE performance_reviews ADD COLUMN IF NOT EXISTS created_by INT;
            ALTER TABLE performance_reviews ADD COLUMN IF NOT EXISTS updated_by INT;
            ALTER TABLE performance_reviews ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();
        ");

        // 3. Seed Tenants
        var now = DateTime.UtcNow;
        var t1 = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == 1);
        if (t1 == null)
        {
            t1 = new Tenant
            {
                Id = 1,
                Name = "CodingEra Technologies",
                Code = "codingera",
                Domain = "codingera.com",
                ContactEmail = "contact@codingera.com",
                ContactPhone = "+91 9876543210",
                Status = "Active",
                Plan = "Enterprise",
                MaxEmployees = 500,
                SubscriptionExpiresAt = now.AddYears(2),
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Tenants.Add(t1);
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync(
                "SELECT setval(pg_get_serial_sequence('tenants', 'id'), COALESCE((SELECT MAX(id) FROM tenants), 0), true)");
        }

        var t2 = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == 2);
        if (t2 == null)
        {
            t2 = new Tenant
            {
                Id = 2,
                Name = "Acme Corporation",
                Code = "acme",
                Domain = "acme.com",
                ContactEmail = "admin@acme.com",
                ContactPhone = "+1 800 555 0199",
                Status = "Active",
                Plan = "Professional",
                MaxEmployees = 150,
                SubscriptionExpiresAt = now.AddYears(1),
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Tenants.Add(t2);
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync(
                "SELECT setval(pg_get_serial_sequence('tenants', 'id'), COALESCE((SELECT MAX(id) FROM tenants), 0), true)");
        }

        // 4. Backfill any existing null tenant_id records to Tenant 1
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE employees SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE branches SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE departments SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE designations SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE attendance_records SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE leave_types SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE leave_allocations SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE leave_requests SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE leave_notifications SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE document_categories SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE employee_documents SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE payroll_records SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE performance_reviews SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
            UPDATE company_assets SET tenant_id = 1 WHERE tenant_id IS NULL OR tenant_id = 0;
        ");

        // 5. Seed Tenant 2 Demo Branch, Dept, Designation & HR Admin
        if (!await db.Branches.IgnoreQueryFilters().AnyAsync(x => x.TenantId == 2))
        {
            var acmeBranch = new Branch
            {
                TenantId = 2,
                Name = "Acme Global HQ - New York",
                Code = "NY_HQ",
                Address = "100 Broadway, New York, NY",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Branches.Add(acmeBranch);
            await db.SaveChangesAsync();

            var acmeDept = new Department
            {
                TenantId = 2,
                BranchId = acmeBranch.Id,
                Name = "Global Operations",
                Code = "OPS",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Departments.Add(acmeDept);

            var acmeDesig = new Designation
            {
                TenantId = 2,
                DepartmentId = null,
                Name = "Operations Specialist",
                Code = "OPS_SPEC",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Designations.Add(acmeDesig);

            var acmeLeaveType = new LeaveType
            {
                TenantId = 2,
                Name = "Paid Time Off",
                Code = "PTO",
                DefaultAllocatedDays = 20,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.LeaveTypes.Add(acmeLeaveType);

            var acmeDocCat = new DocumentCategory
            {
                TenantId = 2,
                Name = "Tax / W-2 Forms",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.DocumentCategories.Add(acmeDocCat);

            await db.SaveChangesAsync();

            // Seed an Employee in Tenant 2
            var acmeEmp = new Employee
            {
                TenantId = 2,
                EmployeeCode = "ACME-1001",
                FullName = "John Doe",
                Email = "john.doe@acme.com",
                PhoneNumber = "+1 555 0100",
                DepartmentId = acmeDept.Id,
                DesignationId = acmeDesig.Id,
                BranchId = acmeBranch.Id,
                Status = "Active",
                JoiningDate = new DateOnly(2024, 1, 15),
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Employees.Add(acmeEmp);
            await db.SaveChangesAsync();

            // Seed Demo Asset for Tenant 2
            db.CompanyAssets.Add(new CompanyAsset
            {
                TenantId = 2,
                BranchId = acmeBranch.Id,
                AssetCode = "ACME-MAC-01",
                Name = "MacBook Pro 16-inch M3",
                Category = "Laptop",
                SerialNumber = "C02XYZ123456",
                Status = "Assigned",
                PurchaseDate = new DateOnly(2024, 2, 1),
                AssignedToEmployeeId = acmeEmp.Id,
                AssignedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
            await db.SaveChangesAsync();
        }

        // 6. Ensure default demo assets exist for Tenant 1
        if (!await db.CompanyAssets.IgnoreQueryFilters().AnyAsync(x => x.TenantId == 1))
        {
            var emp1 = await db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == 1);
            db.CompanyAssets.AddRange(
                new CompanyAsset
                {
                    TenantId = 1,
                    AssetCode = "DEV-LAP-001",
                    Name = "Dell XPS 15 (i9 32GB RAM)",
                    Category = "Laptop",
                    SerialNumber = "DL-XPS-99214",
                    Status = emp1 != null ? "Assigned" : "Available",
                    PurchaseDate = new DateOnly(2024, 1, 10),
                    AssignedToEmployeeId = emp1?.Id,
                    AssignedAt = emp1 != null ? now : null,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new CompanyAsset
                {
                    TenantId = 1,
                    AssetCode = "MON-4K-002",
                    Name = "Dell UltraSharp 27 4K Monitor",
                    Category = "Monitor",
                    SerialNumber = "DL-U2723QE-11",
                    Status = "Available",
                    PurchaseDate = new DateOnly(2024, 3, 5),
                    CreatedAt = now,
                    UpdatedAt = now
                }
            );
            await db.SaveChangesAsync();
        }

        // 7. Configure and sync all Users
        await SeedUsersAsync(db);
    }

    private static async Task SeedUsersAsync(HRMSDbContext db)
    {
        var now = DateTime.UtcNow;
        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");

        // Super Admin (TenantId = null)
        var superAdmin = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == "superadmin@hrms.com");
        if (superAdmin == null)
        {
            db.Users.Add(new User
            {
                TenantId = null,
                FullName = "Super Admin",
                Email = "superadmin@hrms.com",
                PasswordHash = defaultPasswordHash,
                RoleId = RoleSeed.SuperAdmin.Id,
                Status = "Active",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            superAdmin.TenantId = null;
            superAdmin.RoleId = RoleSeed.SuperAdmin.Id;
            superAdmin.Status = "Active";
            superAdmin.IsActive = true;
        }

        // HR Admin 1 for CodingEra (TenantId = 1)
        var hrAdmin1 = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == "hradmin@hrms.com");
        if (hrAdmin1 == null)
        {
            db.Users.Add(new User
            {
                TenantId = 1,
                FullName = "HR Admin (CodingEra)",
                Email = "hradmin@hrms.com",
                PasswordHash = defaultPasswordHash,
                RoleId = RoleSeed.HrAdmin.Id,
                Status = "Active",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            hrAdmin1.TenantId = 1;
            hrAdmin1.FullName = "HR Admin (CodingEra)";
            hrAdmin1.RoleId = RoleSeed.HrAdmin.Id;
            hrAdmin1.Status = "Active";
            hrAdmin1.IsActive = true;
        }

        // HR Admin 2 for Acme Corp (TenantId = 2)
        var hrAdmin2 = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == "hradmin@acme.com");
        if (hrAdmin2 == null)
        {
            db.Users.Add(new User
            {
                TenantId = 2,
                FullName = "HR Admin (Acme Corp)",
                Email = "hradmin@acme.com",
                PasswordHash = defaultPasswordHash,
                RoleId = RoleSeed.HrAdmin.Id,
                Status = "Active",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            hrAdmin2.TenantId = 2;
            hrAdmin2.FullName = "HR Admin (Acme Corp)";
            hrAdmin2.RoleId = RoleSeed.HrAdmin.Id;
            hrAdmin2.Status = "Active";
            hrAdmin2.IsActive = true;
        }

        await db.SaveChangesAsync();

        // Ensure all Employees across all tenants have corresponding User accounts
        var employees = await db.Employees.IgnoreQueryFilters().ToListAsync();
        foreach (var emp in employees)
        {
            var empUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email.ToLower() == emp.Email.ToLower());
            if (empUser == null)
            {
                db.Users.Add(new User
                {
                    TenantId = emp.TenantId,
                    EmployeeId = emp.Id,
                    FullName = emp.FullName,
                    Email = emp.Email.ToLower(),
                    PasswordHash = defaultPasswordHash,
                    RoleId = RoleSeed.Employee.Id,
                    Status = emp.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) ? "Active" : "Deactivated",
                    IsActive = emp.Status.Equals("Active", StringComparison.OrdinalIgnoreCase),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                empUser.TenantId = emp.TenantId;
                empUser.EmployeeId = emp.Id;
                empUser.FullName = emp.FullName;
                if (string.IsNullOrWhiteSpace(empUser.PasswordHash))
                {
                    empUser.PasswordHash = defaultPasswordHash;
                }
            }
        }

        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync(
            "SELECT setval(pg_get_serial_sequence('users', 'id'), COALESCE((SELECT MAX(id) FROM users), 0), true)");
    }
}
