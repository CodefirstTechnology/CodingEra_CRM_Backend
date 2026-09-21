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

            CREATE TABLE IF NOT EXISTS company_profiles (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL UNIQUE,
                company_name VARCHAR(256) NOT NULL,
                legal_name VARCHAR(256),
                company_code VARCHAR(64),
                registration_number VARCHAR(128),
                industry VARCHAR(128),
                company_type VARCHAR(64),
                website VARCHAR(256),
                email VARCHAR(256),
                phone VARCHAR(64),
                address_line1 VARCHAR(256),
                address_line2 VARCHAR(256),
                city VARCHAR(128),
                state VARCHAR(128),
                country VARCHAR(128),
                pin_code VARCHAR(32),
                primary_contact_person VARCHAR(128),
                contact_email VARCHAR(256),
                contact_phone VARCHAR(64),
                pan VARCHAR(32),
                tan VARCHAR(32),
                gstin VARCHAR(32),
                pf_registration_number VARCHAR(64),
                esic_registration_number VARCHAR(64),
                pt_registration_details VARCHAR(128),
                logo_url VARCHAR(512),
                default_currency VARCHAR(16) DEFAULT 'INR',
                timezone VARCHAR(64) DEFAULT 'Asia/Kolkata',
                date_format VARCHAR(32) DEFAULT 'DD/MM/YYYY',
                financial_year VARCHAR(32) DEFAULT 'Apr - Mar',
                payroll_cycle VARCHAR(32) DEFAULT 'Monthly (1st - 30th/31st)',
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS grades (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                grade_code VARCHAR(32) NOT NULL,
                grade_name VARCHAR(128) NOT NULL,
                level INT NOT NULL DEFAULT 1,
                description VARCHAR(256),
                min_salary NUMERIC(14,2),
                max_salary NUMERIC(14,2),
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS cost_centers (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                cost_center_code VARCHAR(32) NOT NULL,
                cost_center_name VARCHAR(128) NOT NULL,
                department_id INT,
                branch_id INT,
                manager_id INT,
                description VARCHAR(256),
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS shifts (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                shift_code VARCHAR(32) NOT NULL,
                shift_name VARCHAR(128) NOT NULL,
                start_time VARCHAR(16) NOT NULL DEFAULT '09:00',
                end_time VARCHAR(16) NOT NULL DEFAULT '18:00',
                grace_period_minutes INT NOT NULL DEFAULT 15,
                break_duration_minutes INT NOT NULL DEFAULT 60,
                working_hours NUMERIC(4,2) NOT NULL DEFAULT 8.0,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS holidays (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                holiday_name VARCHAR(128) NOT NULL,
                holiday_date DATE NOT NULL,
                holiday_type VARCHAR(64) NOT NULL DEFAULT 'Public Holiday',
                branch_id INT,
                is_optional BOOLEAN NOT NULL DEFAULT FALSE,
                description VARCHAR(256),
                status VARCHAR(32) NOT NULL DEFAULT 'Active',
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS working_day_configs (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                branch_id INT,
                day_of_week INT NOT NULL,
                day_name VARCHAR(32) NOT NULL,
                is_working_day BOOLEAN NOT NULL DEFAULT TRUE,
                is_half_day BOOLEAN NOT NULL DEFAULT FALSE,
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS employee_transfers (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                employee_id INT NOT NULL,
                current_branch_id INT,
                new_branch_id INT,
                current_department_id INT,
                new_department_id INT,
                current_designation_id INT,
                new_designation_id INT,
                current_reporting_manager_id INT,
                new_reporting_manager_id INT,
                current_cost_center_id INT,
                new_cost_center_id INT,
                effective_date DATE NOT NULL,
                reason VARCHAR(512) NOT NULL,
                remarks VARCHAR(512),
                requested_by_user_id INT,
                approved_by_user_id INT,
                status VARCHAR(32) NOT NULL DEFAULT 'Pending',
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS employee_promotions (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                employee_id INT NOT NULL,
                current_designation_id INT,
                new_designation_id INT,
                current_grade_id INT,
                new_grade_id INT,
                current_department_id INT,
                new_department_id INT,
                current_salary NUMERIC(14,2),
                new_salary NUMERIC(14,2),
                salary_revision_reference VARCHAR(64),
                effective_date DATE NOT NULL,
                reason VARCHAR(512) NOT NULL,
                remarks VARCHAR(512),
                requested_by_user_id INT,
                approved_by_user_id INT,
                status VARCHAR(32) NOT NULL DEFAULT 'Pending',
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS salary_revision_requests (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                employee_id INT NOT NULL,
                current_salary NUMERIC(14,2) NOT NULL,
                new_salary NUMERIC(14,2) NOT NULL,
                effective_date DATE NOT NULL,
                revision_type VARCHAR(64) NOT NULL DEFAULT 'Annual Increment',
                reason VARCHAR(512) NOT NULL,
                remarks VARCHAR(512),
                requested_by_user_id INT,
                approved_by_user_id INT,
                status VARCHAR(32) NOT NULL DEFAULT 'Draft',
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS employee_exits (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                employee_id INT NOT NULL,
                exit_type VARCHAR(64) NOT NULL DEFAULT 'RESIGNATION',
                resignation_date DATE NOT NULL,
                notice_period_days INT NOT NULL DEFAULT 30,
                last_working_date DATE NOT NULL,
                reason VARCHAR(512) NOT NULL,
                remarks VARCHAR(512),
                status VARCHAR(32) NOT NULL DEFAULT 'Initiated',
                asset_clearance_status VARCHAR(32) NOT NULL DEFAULT 'Pending',
                leave_settlement_status VARCHAR(32) NOT NULL DEFAULT 'Pending',
                payroll_settlement_status VARCHAR(32) NOT NULL DEFAULT 'Pending',
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS employee_lifecycle_histories (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                employee_id INT NOT NULL,
                event_type VARCHAR(128) NOT NULL,
                old_value VARCHAR(512),
                new_value VARCHAR(512),
                reason VARCHAR(512),
                changed_by_user_id INT,
                changed_by_name VARCHAR(128),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS job_requisitions (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                title VARCHAR(128) NOT NULL,
                department_id INT,
                branch_id INT,
                openings_count INT NOT NULL DEFAULT 1,
                min_experience_years INT NOT NULL DEFAULT 0,
                job_description TEXT,
                target_date DATE,
                status VARCHAR(32) NOT NULL DEFAULT 'Open',
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS candidates (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                job_requisition_id INT,
                full_name VARCHAR(128) NOT NULL,
                email VARCHAR(256) NOT NULL,
                phone_number VARCHAR(32) NOT NULL,
                position VARCHAR(128) NOT NULL,
                department_id INT,
                experience_years NUMERIC(4,1) NOT NULL DEFAULT 0,
                resume_path VARCHAR(512),
                source VARCHAR(64),
                status VARCHAR(32) NOT NULL DEFAULT 'APPLIED',
                notes VARCHAR(512),
                joined_employee_id INT,
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS onboarding_tasks (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                employee_id INT,
                task_name VARCHAR(256) NOT NULL,
                category VARCHAR(64) NOT NULL DEFAULT 'Profile',
                is_mandatory BOOLEAN NOT NULL DEFAULT TRUE,
                is_completed BOOLEAN NOT NULL DEFAULT FALSE,
                completed_at TIMESTAMPTZ,
                notes VARCHAR(512),
                created_by INT,
                updated_by INT,
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

            CREATE TABLE IF NOT EXISTS document_types (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                name VARCHAR(128) NOT NULL,
                code VARCHAR(64) NOT NULL,
                category VARCHAR(64) NOT NULL DEFAULT 'Other',
                description VARCHAR(512),
                is_required BOOLEAN NOT NULL DEFAULT TRUE,
                is_mandatory_during_onboarding BOOLEAN NOT NULL DEFAULT FALSE,
                requires_verification BOOLEAN NOT NULL DEFAULT TRUE,
                has_expiry BOOLEAN NOT NULL DEFAULT FALSE,
                default_validity_period_months INT,
                allowed_file_types VARCHAR(128) NOT NULL DEFAULT 'pdf,jpg,jpeg,png',
                maximum_file_size_mb INT NOT NULL DEFAULT 5,
                status VARCHAR(32) NOT NULL DEFAULT 'Active',
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS document_requirements (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                document_type_id INT NOT NULL,
                employment_type VARCHAR(64),
                department_id INT,
                designation_id INT,
                branch_id INT,
                gender VARCHAR(32),
                is_mandatory BOOLEAN NOT NULL DEFAULT TRUE,
                is_onboarding_mandatory BOOLEAN NOT NULL DEFAULT TRUE,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                remarks VARCHAR(512),
                created_by INT,
                updated_by INT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS document_versions (
                id SERIAL PRIMARY KEY,
                tenant_id INT NOT NULL DEFAULT 1,
                employee_document_id INT NOT NULL,
                employee_id INT NOT NULL,
                document_type_id INT,
                version_number INT NOT NULL DEFAULT 1,
                file_path VARCHAR(512) NOT NULL,
                file_name VARCHAR(256) NOT NULL,
                file_size BIGINT NOT NULL DEFAULT 0,
                mime_type VARCHAR(128),
                document_number VARCHAR(128),
                issue_date DATE,
                expiry_date DATE,
                status VARCHAR(64) NOT NULL DEFAULT 'Uploaded',
                rejection_reason VARCHAR(512),
                uploaded_by_user_id INT,
                uploaded_by_name VARCHAR(128),
                uploaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                verified_by_user_id INT,
                verified_by_name VARCHAR(128),
                verified_at TIMESTAMPTZ,
                remarks VARCHAR(512)
            );
        ");

        // 2. Add columns to existing tables
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS first_name VARCHAR(128);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS middle_name VARCHAR(128);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS last_name VARCHAR(128);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS work_email VARCHAR(256);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS alternate_phone VARCHAR(32);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS grade_id INT;
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS cost_center_id INT;
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS shift_id INT;
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS reporting_manager_id INT;
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS work_location VARCHAR(128);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS employment_type VARCHAR(64) DEFAULT 'Full Time';
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS marital_status VARCHAR(32);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS profile_photo_url VARCHAR(512);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS current_address VARCHAR(512);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS permanent_address VARCHAR(512);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS emergency_contact_name VARCHAR(128);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS emergency_contact_phone VARCHAR(32);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS pan VARCHAR(32);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS aadhaar VARCHAR(32);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS passport_number VARCHAR(32);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS driving_license VARCHAR(32);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS uan VARCHAR(32);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS esic_number VARCHAR(32);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS account_holder_name VARCHAR(128);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS current_ctc NUMERIC(14,2);

            ALTER TABLE branches ADD COLUMN IF NOT EXISTS branch_type VARCHAR(64) DEFAULT 'Office';
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS city VARCHAR(128);
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS state VARCHAR(128);
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS country VARCHAR(128);
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS pin_code VARCHAR(32);
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS contact_person VARCHAR(128);
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS contact_email VARCHAR(256);
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS contact_phone VARCHAR(64);
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS working_hours VARCHAR(64) DEFAULT '09:00 - 18:00';
            ALTER TABLE branches ADD COLUMN IF NOT EXISTS timezone VARCHAR(64) DEFAULT 'Asia/Kolkata';

            ALTER TABLE departments ADD COLUMN IF NOT EXISTS description VARCHAR(256);
            ALTER TABLE departments ADD COLUMN IF NOT EXISTS department_head_id INT;
            ALTER TABLE departments ADD COLUMN IF NOT EXISTS parent_department_id INT;

            ALTER TABLE designations ADD COLUMN IF NOT EXISTS grade_id INT;
            ALTER TABLE designations ADD COLUMN IF NOT EXISTS description VARCHAR(256);
            ALTER TABLE designations ADD COLUMN IF NOT EXISTS min_experience_years NUMERIC(4,1);

            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS document_type_id INT;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS document_number VARCHAR(128);
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS issue_date DATE;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS expiry_date DATE;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS file_name VARCHAR(256);
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS file_size BIGINT DEFAULT 0;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS mime_type VARCHAR(128);
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS version_number INT DEFAULT 1;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS is_current BOOLEAN DEFAULT TRUE;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS status VARCHAR(64) DEFAULT 'Pending Verification';
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS rejection_reason VARCHAR(512);
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS verified_by_user_id INT;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS verified_by_name VARCHAR(128);
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS verified_at TIMESTAMPTZ;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS remarks VARCHAR(512);
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS is_archived BOOLEAN DEFAULT FALSE;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS updated_by INT;
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ DEFAULT NOW();
            ALTER TABLE employee_documents ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();
        ");

        // 3. Seed Default Company Profiles, Grades, Shifts, Working Days for Tenant 1 & Tenant 2
        var now = DateTime.UtcNow;
        if (!await db.CompanyProfiles.IgnoreQueryFilters().AnyAsync(x => x.TenantId == 1))
        {
            db.CompanyProfiles.Add(new CompanyProfile
            {
                TenantId = 1,
                CompanyName = "CodingEra Technologies Pvt. Ltd.",
                LegalName = "CodingEra Technologies Private Limited",
                CompanyCode = "CODINGERA",
                RegistrationNumber = "U72200MH2024PTC123456",
                Industry = "Information Technology & Software Services",
                CompanyType = "Private Limited",
                Website = "https://codingera.com",
                Email = "hr@codingera.com",
                Phone = "+91 22 1234 5678",
                AddressLine1 = "Level 5, Tech Park, Powai",
                City = "Mumbai",
                State = "Maharashtra",
                Country = "India",
                PinCode = "400076",
                PrimaryContactPerson = "Sagar Shinde",
                ContactEmail = "sagar@codingera.com",
                ContactPhone = "+91 9876543210",
                Pan = "AAACC1234C",
                Tan = "MUMA12345C",
                Gstin = "27AAACC1234C1Z5",
                PfRegistrationNumber = "MH/BAN/0012345/000",
                EsicRegistrationNumber = "31000123450000999",
                PtRegistrationDetails = "PTR-MH-998877",
                DefaultCurrency = "INR",
                Timezone = "Asia/Kolkata",
                DateFormat = "DD/MM/YYYY",
                FinancialYear = "Apr - Mar",
                PayrollCycle = "Monthly (1st - 30th/31st)",
                CreatedAt = now,
                UpdatedAt = now
            });
            await db.SaveChangesAsync();
        }

        // Seed Default Grades for Tenant 1
        if (!await db.Grades.IgnoreQueryFilters().AnyAsync(x => x.TenantId == 1))
        {
            db.Grades.AddRange(
                new Grade { TenantId = 1, GradeCode = "L1", GradeName = "Associate / Junior", Level = 1, Description = "Entry level engineers and associates", MinSalary = 300000, MaxSalary = 600000, CreatedAt = now, UpdatedAt = now },
                new Grade { TenantId = 1, GradeCode = "L2", GradeName = "Senior Executive / Developer", Level = 2, Description = "Mid-level independent contributors", MinSalary = 600000, MaxSalary = 1200000, CreatedAt = now, UpdatedAt = now },
                new Grade { TenantId = 1, GradeCode = "L3", GradeName = "Lead / Specialist", Level = 3, Description = "Team leads and domain experts", MinSalary = 1200000, MaxSalary = 2000000, CreatedAt = now, UpdatedAt = now },
                new Grade { TenantId = 1, GradeCode = "L4", GradeName = "Principal / Engineering Manager", Level = 4, Description = "Engineering management and staff specialists", MinSalary = 2000000, MaxSalary = 3500000, CreatedAt = now, UpdatedAt = now },
                new Grade { TenantId = 1, GradeCode = "L5", GradeName = "Director / VP", Level = 5, Description = "Executive leadership and department heads", MinSalary = 3500000, MaxSalary = 6000000, CreatedAt = now, UpdatedAt = now }
            );
            await db.SaveChangesAsync();
        }

        // Seed Default Shifts for Tenant 1
        if (!await db.Shifts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == 1))
        {
            db.Shifts.AddRange(
                new Shift { TenantId = 1, ShiftCode = "GEN", ShiftName = "General Shift", StartTime = "09:30", EndTime = "18:30", GracePeriodMinutes = 15, BreakDurationMinutes = 60, WorkingHours = 8.0m, CreatedAt = now, UpdatedAt = now },
                new Shift { TenantId = 1, ShiftCode = "MORN", ShiftName = "Morning Shift", StartTime = "07:00", EndTime = "16:00", GracePeriodMinutes = 15, BreakDurationMinutes = 60, WorkingHours = 8.0m, CreatedAt = now, UpdatedAt = now },
                new Shift { TenantId = 1, ShiftCode = "EVE", ShiftName = "Evening Shift", StartTime = "15:00", EndTime = "00:00", GracePeriodMinutes = 15, BreakDurationMinutes = 60, WorkingHours = 8.0m, CreatedAt = now, UpdatedAt = now }
            );
            await db.SaveChangesAsync();
        }

        // Seed Default Working Days for Tenant 1
        if (!await db.WorkingDayConfigs.IgnoreQueryFilters().AnyAsync(x => x.TenantId == 1))
        {
            db.WorkingDayConfigs.AddRange(
                new WorkingDayConfig { TenantId = 1, DayOfWeek = 1, DayName = "Monday", IsWorkingDay = true, IsHalfDay = false, CreatedAt = now, UpdatedAt = now },
                new WorkingDayConfig { TenantId = 1, DayOfWeek = 2, DayName = "Tuesday", IsWorkingDay = true, IsHalfDay = false, CreatedAt = now, UpdatedAt = now },
                new WorkingDayConfig { TenantId = 1, DayOfWeek = 3, DayName = "Wednesday", IsWorkingDay = true, IsHalfDay = false, CreatedAt = now, UpdatedAt = now },
                new WorkingDayConfig { TenantId = 1, DayOfWeek = 4, DayName = "Thursday", IsWorkingDay = true, IsHalfDay = false, CreatedAt = now, UpdatedAt = now },
                new WorkingDayConfig { TenantId = 1, DayOfWeek = 5, DayName = "Friday", IsWorkingDay = true, IsHalfDay = false, CreatedAt = now, UpdatedAt = now },
                new WorkingDayConfig { TenantId = 1, DayOfWeek = 6, DayName = "Saturday", IsWorkingDay = false, IsHalfDay = false, CreatedAt = now, UpdatedAt = now },
                new WorkingDayConfig { TenantId = 1, DayOfWeek = 0, DayName = "Sunday", IsWorkingDay = false, IsHalfDay = false, CreatedAt = now, UpdatedAt = now }
            );
            await db.SaveChangesAsync();
        }

        // Seed Default Holidays for Tenant 1 (Year 2026)
        if (!await db.Holidays.IgnoreQueryFilters().AnyAsync(x => x.TenantId == 1))
        {
            db.Holidays.AddRange(
                new Holiday { TenantId = 1, HolidayName = "Republic Day", HolidayDate = new DateOnly(2026, 1, 26), HolidayType = "National Holiday", Description = "National Republic Day celebration", CreatedAt = now, UpdatedAt = now },
                new Holiday { TenantId = 1, HolidayName = "Holi (Festival of Colours)", HolidayDate = new DateOnly(2026, 3, 4), HolidayType = "Public Holiday", Description = "Cultural holiday", CreatedAt = now, UpdatedAt = now },
                new Holiday { TenantId = 1, HolidayName = "Independence Day", HolidayDate = new DateOnly(2026, 8, 15), HolidayType = "National Holiday", Description = "Indian Independence Day", CreatedAt = now, UpdatedAt = now },
                new Holiday { TenantId = 1, HolidayName = "Mahatma Gandhi Jayanti", HolidayDate = new DateOnly(2026, 10, 2), HolidayType = "National Holiday", Description = "National Holiday", CreatedAt = now, UpdatedAt = now },
                new Holiday { TenantId = 1, HolidayName = "Dussehra", HolidayDate = new DateOnly(2026, 10, 20), HolidayType = "Public Holiday", Description = "Vijayadashami", CreatedAt = now, UpdatedAt = now },
                new Holiday { TenantId = 1, HolidayName = "Diwali (Deepavali)", HolidayDate = new DateOnly(2026, 11, 8), HolidayType = "Public Holiday", Description = "Festival of Lights", CreatedAt = now, UpdatedAt = now }
            );
            await db.SaveChangesAsync();
        }

        // 4. Default Onboarding Checklist Templates
        if (!await db.OnboardingTasks.IgnoreQueryFilters().AnyAsync(x => x.TenantId == 1))
        {
            db.OnboardingTasks.AddRange(
                new OnboardingTask { TenantId = 1, TaskName = "Complete Employee Personal & Emergency Details", Category = "Profile", IsMandatory = true, CreatedAt = now, UpdatedAt = now },
                new OnboardingTask { TenantId = 1, TaskName = "Submit Identity Proofs (PAN Card & Aadhaar/Passport)", Category = "Identity Documents", IsMandatory = true, CreatedAt = now, UpdatedAt = now },
                new OnboardingTask { TenantId = 1, TaskName = "Provide Bank Account & Canceled Cheque", Category = "Bank Details", IsMandatory = true, CreatedAt = now, UpdatedAt = now },
                new OnboardingTask { TenantId = 1, TaskName = "Acknowledge Code of Conduct & NDA", Category = "Policy Agreement", IsMandatory = true, CreatedAt = now, UpdatedAt = now },
                new OnboardingTask { TenantId = 1, TaskName = "IT Laptop & Hardware Handover Acknowledgment", Category = "Asset Allocation", IsMandatory = true, CreatedAt = now, UpdatedAt = now },
                new OnboardingTask { TenantId = 1, TaskName = "Assign Work Email & VPN System Access", Category = "System Access", IsMandatory = true, CreatedAt = now, UpdatedAt = now }
            );
            await db.SaveChangesAsync();
        }

        // 5. Seed Default Document Types for Tenant 1
        if (!await db.DocumentTypes.IgnoreQueryFilters().AnyAsync(x => x.TenantId == 1))
        {
            var docTypes = new List<DocumentType>
            {
                new DocumentType { TenantId = 1, Name = "Aadhaar Card", Code = "AADHAAR", Category = "Identity", Description = "Government issued 12-digit unique identity card", IsRequired = true, IsMandatoryDuringOnboarding = true, RequiresVerification = true, HasExpiry = false, MaximumFileSizeMb = 5, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now },
                new DocumentType { TenantId = 1, Name = "PAN Card", Code = "PAN", Category = "Tax", Description = "Permanent Account Number issued by Income Tax Department", IsRequired = true, IsMandatoryDuringOnboarding = true, RequiresVerification = true, HasExpiry = false, MaximumFileSizeMb = 5, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now },
                new DocumentType { TenantId = 1, Name = "Passport", Code = "PASSPORT", Category = "Identity", Description = "National passport for international travel & identity proof", IsRequired = false, IsMandatoryDuringOnboarding = false, RequiresVerification = true, HasExpiry = true, DefaultValidityPeriodMonths = 120, MaximumFileSizeMb = 5, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now },
                new DocumentType { TenantId = 1, Name = "Driving License", Code = "DL", Category = "Identity", Description = "State transport motor vehicle driving license", IsRequired = false, IsMandatoryDuringOnboarding = false, RequiresVerification = true, HasExpiry = true, DefaultValidityPeriodMonths = 240, MaximumFileSizeMb = 5, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now },
                new DocumentType { TenantId = 1, Name = "Degree / Graduation Certificate", Code = "DEGREE_CERT", Category = "Education", Description = "Final Degree / Diploma graduation certificate", IsRequired = true, IsMandatoryDuringOnboarding = true, RequiresVerification = true, HasExpiry = false, MaximumFileSizeMb = 10, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now },
                new DocumentType { TenantId = 1, Name = "Previous Employment Relieving Letter", Code = "RELIEVING_LETTER", Category = "Employment", Description = "Formal relieving letter from last employer", IsRequired = true, IsMandatoryDuringOnboarding = true, RequiresVerification = true, HasExpiry = false, MaximumFileSizeMb = 10, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now },
                new DocumentType { TenantId = 1, Name = "Experience / Service Certificate", Code = "EXP_CERT", Category = "Employment", Description = "Work experience certificate from prior companies", IsRequired = true, IsMandatoryDuringOnboarding = false, RequiresVerification = true, HasExpiry = false, MaximumFileSizeMb = 10, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now },
                new DocumentType { TenantId = 1, Name = "Company Offer & Appointment Letter", Code = "APPOINTMENT_LTR", Category = "Joining", Description = "Signed offer and employment appointment letter", IsRequired = true, IsMandatoryDuringOnboarding = true, RequiresVerification = true, HasExpiry = false, MaximumFileSizeMb = 10, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now },
                new DocumentType { TenantId = 1, Name = "Bank Account Proof / Cancelled Cheque", Code = "BANK_PROOF", Category = "Banking", Description = "Bank passbook copy or cancelled cheque for salary credit", IsRequired = true, IsMandatoryDuringOnboarding = true, RequiresVerification = true, HasExpiry = false, MaximumFileSizeMb = 5, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now },
                new DocumentType { TenantId = 1, Name = "Last 3 Months Payslips / Form 16", Code = "SALARY_PROOF", Category = "Tax", Description = "Salary payslips from previous employer or tax Form 16", IsRequired = false, IsMandatoryDuringOnboarding = false, RequiresVerification = true, HasExpiry = false, MaximumFileSizeMb = 10, Status = "Active", IsActive = true, CreatedAt = now, UpdatedAt = now }
            };

            db.DocumentTypes.AddRange(docTypes);
            await db.SaveChangesAsync();

            // Seed Document Requirements
            var aadhaarType = docTypes.First(d => d.Code == "AADHAAR");
            var panType = docTypes.First(d => d.Code == "PAN");
            var degreeType = docTypes.First(d => d.Code == "DEGREE_CERT");
            var bankType = docTypes.First(d => d.Code == "BANK_PROOF");
            var relievingType = docTypes.First(d => d.Code == "RELIEVING_LETTER");

            db.DocumentRequirements.AddRange(
                new DocumentRequirement { TenantId = 1, DocumentTypeId = aadhaarType.Id, EmploymentType = null, IsMandatory = true, IsOnboardingMandatory = true, IsActive = true, Remarks = "Mandatory KYC proof for all employees", CreatedAt = now, UpdatedAt = now },
                new DocumentRequirement { TenantId = 1, DocumentTypeId = panType.Id, EmploymentType = null, IsMandatory = true, IsOnboardingMandatory = true, IsActive = true, Remarks = "Mandatory for tax deduction and PF compliance", CreatedAt = now, UpdatedAt = now },
                new DocumentRequirement { TenantId = 1, DocumentTypeId = degreeType.Id, EmploymentType = null, IsMandatory = true, IsOnboardingMandatory = true, IsActive = true, Remarks = "Highest qualification degree proof", CreatedAt = now, UpdatedAt = now },
                new DocumentRequirement { TenantId = 1, DocumentTypeId = bankType.Id, EmploymentType = null, IsMandatory = true, IsOnboardingMandatory = true, IsActive = true, Remarks = "Required for monthly payroll disbursement", CreatedAt = now, UpdatedAt = now },
                new DocumentRequirement { TenantId = 1, DocumentTypeId = relievingType.Id, EmploymentType = "Full Time", IsMandatory = true, IsOnboardingMandatory = true, IsActive = true, Remarks = "Required for experienced hires", CreatedAt = now, UpdatedAt = now }
            );
            await db.SaveChangesAsync();
        }
    }
}
