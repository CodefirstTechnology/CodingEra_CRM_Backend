using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/organization")]
[ApiController]
public class OrganizationController : ControllerBase
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;

    public OrganizationController(HRMSDbContext context, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    // =========================================================================
    // 1. ORGANIZATION SUMMARY
    // =========================================================================
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var profile = await _context.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var companyName = profile?.CompanyName ?? "HRMS Company";
        var legalName = profile?.LegalName ?? companyName;
        var companyCode = profile?.CompanyCode ?? "ORG";

        var totalBranches = await _context.Branches.CountAsync(cancellationToken);
        var totalDepartments = await _context.Departments.CountAsync(cancellationToken);
        var totalDesignations = await _context.Designations.CountAsync(cancellationToken);
        var totalGrades = await _context.Grades.CountAsync(cancellationToken);
        var totalCostCenters = await _context.CostCenters.CountAsync(cancellationToken);
        var totalShifts = await _context.Shifts.CountAsync(cancellationToken);
        var totalHolidays = await _context.Holidays.CountAsync(cancellationToken);
        var totalEmployees = await _context.Employees.CountAsync(cancellationToken);
        var activeEmployees = await _context.Employees.CountAsync(e => e.Status == "Active", cancellationToken);

        var summary = new OrganizationSummaryDto
        {
            CompanyName = companyName,
            LegalName = legalName,
            CompanyCode = companyCode,
            TotalBranches = totalBranches,
            TotalDepartments = totalDepartments,
            TotalDesignations = totalDesignations,
            TotalGrades = totalGrades,
            TotalCostCenters = totalCostCenters,
            TotalShifts = totalShifts,
            TotalHolidays = totalHolidays,
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees
        };

        return Ok(summary);
    }

    // =========================================================================
    // 2. COMPANY PROFILE
    // =========================================================================
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await _context.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (profile == null)
        {
            return Ok(new CompanyProfileDto
            {
                CompanyName = "My Organization",
                DefaultCurrency = "INR",
                Timezone = "Asia/Kolkata",
                DateFormat = "DD/MM/YYYY",
                FinancialYearStartMonth = 4,
                PayrollCycle = "Monthly"
            });
        }

        return Ok(MapCompanyProfileToDto(profile));
    }

    [HttpPut("profile")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> UpdateProfile([FromBody] CompanyProfileUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var profile = await _context.CompanyProfiles.FirstOrDefaultAsync(cancellationToken);
        if (profile == null)
        {
            profile = new CompanyProfile();
            _context.CompanyProfiles.Add(profile);
        }

        profile.CompanyName = dto.CompanyName.Trim();
        profile.LegalName = dto.LegalName?.Trim();
        profile.CompanyCode = dto.CompanyCode?.Trim();
        profile.RegistrationNumber = dto.RegistrationNumber?.Trim();
        profile.Industry = dto.Industry?.Trim();
        profile.CompanyType = dto.CompanyType?.Trim();
        profile.Website = dto.Website?.Trim();
        profile.Email = dto.Email?.Trim();
        profile.Phone = dto.Phone?.Trim();

        profile.AddressLine1 = dto.AddressLine1?.Trim();
        profile.AddressLine2 = dto.AddressLine2?.Trim();
        profile.City = dto.City?.Trim();
        profile.State = dto.State?.Trim();
        profile.Country = dto.Country?.Trim();
        profile.PinCode = dto.PinCode?.Trim();

        profile.PrimaryContactPerson = dto.PrimaryContactPerson?.Trim();
        profile.ContactEmail = dto.ContactEmail?.Trim();
        profile.ContactPhone = dto.ContactPhone?.Trim();

        profile.Pan = dto.Pan?.Trim();
        profile.Tan = dto.Tan?.Trim();
        profile.Gstin = dto.Gstin?.Trim();
        profile.PfRegistrationNumber = dto.PfRegistrationNumber?.Trim();
        profile.EsicRegistrationNumber = dto.EsicRegistrationNumber?.Trim();
        profile.PtRegistrationDetails = dto.ProfessionalTaxNumber?.Trim();

        profile.LogoUrl = dto.CompanyLogoUrl?.Trim();

        profile.DefaultCurrency = dto.DefaultCurrency?.Trim() ?? "INR";
        profile.Timezone = dto.Timezone?.Trim() ?? "Asia/Kolkata";
        profile.DateFormat = dto.DateFormat?.Trim() ?? "DD/MM/YYYY";
        profile.FinancialYear = $"Month {dto.FinancialYearStartMonth}";
        profile.PayrollCycle = dto.PayrollCycle?.Trim() ?? "Monthly";
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Record audit log
        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = profile.TenantId,
            UserId = _currentUser.UserId ?? 1,
            Action = "UPDATE_COMPANY_PROFILE",
            EntityName = "CompanyProfile",
            EntityId = profile.Id.ToString(),
            Details = $"Updated company profile: {profile.CompanyName}",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapCompanyProfileToDto(profile));
    }

    // =========================================================================
    // 3. GRADES / LEVELS
    // =========================================================================
    [HttpGet("grades")]
    public async Task<IActionResult> GetGrades([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Grades.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(g => g.IsActive);
        }

        var items = await query.OrderBy(g => g.Level).ThenBy(g => g.GradeCode).ToListAsync(cancellationToken);
        return Ok(items.Select(g => new GradeDto
        {
            Id = g.Id,
            GradeCode = g.GradeCode,
            GradeName = g.GradeName,
            Level = g.Level,
            Description = g.Description,
            MinimumSalary = g.MinSalary,
            MaximumSalary = g.MaxSalary,
            IsActive = g.IsActive
        }));
    }

    [HttpPost("grades")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> CreateGrade([FromBody] GradeUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var code = dto.GradeCode.Trim().ToUpperInvariant();
        if (await _context.Grades.AnyAsync(g => g.GradeCode == code, cancellationToken))
        {
            return Conflict("A grade with this code already exists.");
        }

        var grade = new Grade
        {
            GradeCode = code,
            GradeName = dto.GradeName.Trim(),
            Level = dto.Level,
            Description = dto.Description?.Trim(),
            MinSalary = dto.MinimumSalary,
            MaxSalary = dto.MaximumSalary,
            IsActive = dto.IsActive
        };

        _context.Grades.Add(grade);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(grade);
    }

    [HttpPut("grades/{id:int}")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> UpdateGrade(int id, [FromBody] GradeUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (grade == null) return NotFound();

        var code = dto.GradeCode.Trim().ToUpperInvariant();
        if (await _context.Grades.AnyAsync(g => g.GradeCode == code && g.Id != id, cancellationToken))
        {
            return Conflict("A grade with this code already exists.");
        }

        grade.GradeCode = code;
        grade.GradeName = dto.GradeName.Trim();
        grade.Level = dto.Level;
        grade.Description = dto.Description?.Trim();
        grade.MinSalary = dto.MinimumSalary;
        grade.MaxSalary = dto.MaximumSalary;
        grade.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(grade);
    }

    [HttpDelete("grades/{id:int}")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> DeleteGrade(int id, CancellationToken cancellationToken)
    {
        var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (grade == null) return NotFound();

        var isUsed = await _context.Employees.AnyAsync(e => e.GradeId == id, cancellationToken);
        if (isUsed)
        {
            grade.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Grade is in use by employees and has been deactivated." });
        }

        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // =========================================================================
    // 4. COST CENTERS
    // =========================================================================
    [HttpGet("cost-centers")]
    public async Task<IActionResult> GetCostCenters([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.CostCenters.AsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.Branch)
            .Include(c => c.Manager);

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive).Include(c => c.Department).Include(c => c.Branch).Include(c => c.Manager);
        }

        var items = await query.OrderBy(c => c.CostCenterCode).ToListAsync(cancellationToken);
        return Ok(items.Select(c => new CostCenterDto
        {
            Id = c.Id,
            CostCenterCode = c.CostCenterCode,
            CostCenterName = c.CostCenterName,
            DepartmentId = c.DepartmentId,
            DepartmentName = c.Department?.Name,
            BranchId = c.BranchId,
            BranchName = c.Branch?.Name,
            ManagerId = c.ManagerId,
            ManagerName = c.Manager?.FullName,
            Description = c.Description,
            IsActive = c.IsActive
        }));
    }

    [HttpPost("cost-centers")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> CreateCostCenter([FromBody] CostCenterUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var code = dto.CostCenterCode.Trim().ToUpperInvariant();
        if (await _context.CostCenters.AnyAsync(c => c.CostCenterCode == code, cancellationToken))
        {
            return Conflict("A cost center with this code already exists.");
        }

        var costCenter = new CostCenter
        {
            CostCenterCode = code,
            CostCenterName = dto.CostCenterName.Trim(),
            DepartmentId = dto.DepartmentId,
            BranchId = dto.BranchId,
            ManagerId = dto.ManagerId,
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive
        };

        _context.CostCenters.Add(costCenter);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(costCenter);
    }

    [HttpPut("cost-centers/{id:int}")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> UpdateCostCenter(int id, [FromBody] CostCenterUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var costCenter = await _context.CostCenters.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (costCenter == null) return NotFound();

        var code = dto.CostCenterCode.Trim().ToUpperInvariant();
        if (await _context.CostCenters.AnyAsync(c => c.CostCenterCode == code && c.Id != id, cancellationToken))
        {
            return Conflict("A cost center with this code already exists.");
        }

        costCenter.CostCenterCode = code;
        costCenter.CostCenterName = dto.CostCenterName.Trim();
        costCenter.DepartmentId = dto.DepartmentId;
        costCenter.BranchId = dto.BranchId;
        costCenter.ManagerId = dto.ManagerId;
        costCenter.Description = dto.Description?.Trim();
        costCenter.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(costCenter);
    }

    [HttpDelete("cost-centers/{id:int}")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> DeleteCostCenter(int id, CancellationToken cancellationToken)
    {
        var costCenter = await _context.CostCenters.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (costCenter == null) return NotFound();

        var isUsed = await _context.Employees.AnyAsync(e => e.CostCenterId == id, cancellationToken);
        if (isUsed)
        {
            costCenter.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Cost center is in use and has been deactivated." });
        }

        _context.CostCenters.Remove(costCenter);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // =========================================================================
    // 5. SHIFTS
    // =========================================================================
    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Shifts.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var items = await query.OrderBy(s => s.StartTime).ToListAsync(cancellationToken);
        return Ok(items.Select(s => new ShiftDto
        {
            Id = s.Id,
            ShiftCode = s.ShiftCode,
            ShiftName = s.ShiftName,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            GracePeriodMinutes = s.GracePeriodMinutes,
            BreakDurationMinutes = s.BreakDurationMinutes,
            WorkingHours = s.WorkingHours,
            IsActive = s.IsActive
        }));
    }

    [HttpPost("shifts")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> CreateShift([FromBody] ShiftUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var code = dto.ShiftCode.Trim().ToUpperInvariant();
        if (await _context.Shifts.AnyAsync(s => s.ShiftCode == code, cancellationToken))
        {
            return Conflict("A shift with this code already exists.");
        }

        var shift = new Shift
        {
            ShiftCode = code,
            ShiftName = dto.ShiftName.Trim(),
            StartTime = string.IsNullOrWhiteSpace(dto.StartTime) ? "09:00" : dto.StartTime.Trim(),
            EndTime = string.IsNullOrWhiteSpace(dto.EndTime) ? "18:00" : dto.EndTime.Trim(),
            GracePeriodMinutes = dto.GracePeriodMinutes,
            BreakDurationMinutes = dto.BreakDurationMinutes,
            WorkingHours = dto.WorkingHours,
            IsActive = dto.IsActive
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(shift);
    }

    [HttpPut("shifts/{id:int}")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> UpdateShift(int id, [FromBody] ShiftUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (shift == null) return NotFound();

        var code = dto.ShiftCode.Trim().ToUpperInvariant();
        if (await _context.Shifts.AnyAsync(s => s.ShiftCode == code && s.Id != id, cancellationToken))
        {
            return Conflict("A shift with this code already exists.");
        }

        shift.ShiftCode = code;
        shift.ShiftName = dto.ShiftName.Trim();
        shift.StartTime = string.IsNullOrWhiteSpace(dto.StartTime) ? shift.StartTime : dto.StartTime.Trim();
        shift.EndTime = string.IsNullOrWhiteSpace(dto.EndTime) ? shift.EndTime : dto.EndTime.Trim();
        shift.GracePeriodMinutes = dto.GracePeriodMinutes;
        shift.BreakDurationMinutes = dto.BreakDurationMinutes;
        shift.WorkingHours = dto.WorkingHours;
        shift.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(shift);
    }

    [HttpDelete("shifts/{id:int}")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> DeleteShift(int id, CancellationToken cancellationToken)
    {
        var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (shift == null) return NotFound();

        var isUsed = await _context.Employees.AnyAsync(e => e.ShiftId == id, cancellationToken);
        if (isUsed)
        {
            shift.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Shift is assigned to employees and has been deactivated." });
        }

        _context.Shifts.Remove(shift);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // =========================================================================
    // 6. HOLIDAYS
    // =========================================================================
    [HttpGet("holidays")]
    public async Task<IActionResult> GetHolidays([FromQuery] int? branchId, [FromQuery] int? year, CancellationToken cancellationToken = default)
    {
        var query = _context.Holidays.AsNoTracking().Include(h => h.Branch).AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(h => h.BranchId == branchId.Value || h.BranchId == null);
        }

        if (year.HasValue)
        {
            query = query.Where(h => h.HolidayDate.Year == year.Value);
        }

        var items = await query.OrderBy(h => h.HolidayDate).ToListAsync(cancellationToken);
        return Ok(items.Select(h => new HolidayDto
        {
            Id = h.Id,
            HolidayName = h.HolidayName,
            HolidayDate = h.HolidayDate,
            HolidayType = h.HolidayType,
            BranchId = h.BranchId,
            BranchName = h.Branch?.Name,
            IsOptional = h.IsOptional,
            Description = h.Description,
            IsActive = h.Status == "Active"
        }));
    }

    [HttpPost("holidays")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> CreateHoliday([FromBody] HolidayUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var holiday = new Holiday
        {
            HolidayName = dto.HolidayName.Trim(),
            HolidayDate = dto.HolidayDate,
            HolidayType = dto.HolidayType?.Trim() ?? "Public Holiday",
            BranchId = dto.BranchId,
            IsOptional = dto.IsOptional,
            Description = dto.Description?.Trim(),
            Status = dto.IsActive ? "Active" : "Inactive"
        };

        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(holiday);
    }

    [HttpPut("holidays/{id:int}")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> UpdateHoliday(int id, [FromBody] HolidayUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var holiday = await _context.Holidays.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (holiday == null) return NotFound();

        holiday.HolidayName = dto.HolidayName.Trim();
        holiday.HolidayDate = dto.HolidayDate;
        holiday.HolidayType = dto.HolidayType?.Trim() ?? "Public Holiday";
        holiday.BranchId = dto.BranchId;
        holiday.IsOptional = dto.IsOptional;
        holiday.Description = dto.Description?.Trim();
        holiday.Status = dto.IsActive ? "Active" : "Inactive";

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(holiday);
    }

    [HttpDelete("holidays/{id:int}")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> DeleteHoliday(int id, CancellationToken cancellationToken)
    {
        var holiday = await _context.Holidays.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (holiday == null) return NotFound();

        _context.Holidays.Remove(holiday);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // =========================================================================
    // 7. WORKING DAYS CONFIGURATION
    // =========================================================================
    [HttpGet("working-days")]
    public async Task<IActionResult> GetWorkingDays([FromQuery] int? branchId, CancellationToken cancellationToken = default)
    {
        var query = _context.WorkingDayConfigs.AsNoTracking().Include(w => w.Branch).AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(w => w.BranchId == branchId.Value);
        }
        else
        {
            query = query.Where(w => w.BranchId == null);
        }

        var configs = await query.OrderBy(w => w.DayOfWeek).ToListAsync(cancellationToken);

        if (configs.Count == 0)
        {
            // Return standard Monday - Sunday defaults
            var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            var defaults = days.Select((name, idx) => new WorkingDayConfigDto
            {
                DayOfWeek = idx + 1,
                DayName = name,
                IsWorkingDay = idx < 5,
                IsWeeklyOff = idx >= 5
            }).ToList();
            return Ok(defaults);
        }

        return Ok(configs.Select(c => new WorkingDayConfigDto
        {
            Id = c.Id,
            BranchId = c.BranchId,
            BranchName = c.Branch?.Name,
            DayOfWeek = c.DayOfWeek,
            DayName = c.DayName,
            IsWorkingDay = c.IsWorkingDay,
            IsWeeklyOff = !c.IsWorkingDay
        }));
    }

    [HttpPut("working-days")]
    [RequirePermission(HrmsPermissions.OrganizationManage)]
    public async Task<IActionResult> UpdateWorkingDays([FromBody] WorkingDayConfigBatchUpdateDto dto, CancellationToken cancellationToken)
    {
        var existing = await _context.WorkingDayConfigs
            .Where(w => w.BranchId == dto.BranchId)
            .ToListAsync(cancellationToken);

        foreach (var dayItem in dto.Days)
        {
            var matched = existing.FirstOrDefault(e => e.DayOfWeek == dayItem.DayOfWeek);
            if (matched != null)
            {
                matched.DayName = dayItem.DayName;
                matched.IsWorkingDay = dayItem.IsWorkingDay;
                matched.IsHalfDay = false;
            }
            else
            {
                _context.WorkingDayConfigs.Add(new WorkingDayConfig
                {
                    BranchId = dto.BranchId,
                    DayOfWeek = dayItem.DayOfWeek,
                    DayName = dayItem.DayName,
                    IsWorkingDay = dayItem.IsWorkingDay,
                    IsHalfDay = false
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Working days updated successfully." });
    }

    private static CompanyProfileDto MapCompanyProfileToDto(CompanyProfile p) => new()
    {
        Id = p.Id,
        TenantId = p.TenantId,
        CompanyName = p.CompanyName,
        LegalName = p.LegalName,
        CompanyCode = p.CompanyCode,
        RegistrationNumber = p.RegistrationNumber,
        Industry = p.Industry,
        CompanyType = p.CompanyType,
        Website = p.Website,
        Email = p.Email,
        Phone = p.Phone,
        AddressLine1 = p.AddressLine1,
        AddressLine2 = p.AddressLine2,
        City = p.City,
        State = p.State,
        Country = p.Country,
        PinCode = p.PinCode,
        PrimaryContactPerson = p.PrimaryContactPerson,
        ContactEmail = p.ContactEmail,
        ContactPhone = p.ContactPhone,
        Pan = p.Pan,
        Tan = p.Tan,
        Gstin = p.Gstin,
        PfRegistrationNumber = p.PfRegistrationNumber,
        EsicRegistrationNumber = p.EsicRegistrationNumber,
        ProfessionalTaxNumber = p.PtRegistrationDetails,
        CompanyLogoUrl = p.LogoUrl,
        DefaultCurrency = p.DefaultCurrency,
        Timezone = p.Timezone,
        DateFormat = p.DateFormat,
        FinancialYearStartMonth = 4,
        PayrollCycle = p.PayrollCycle,
        UpdatedAt = p.UpdatedAt
    };
}
