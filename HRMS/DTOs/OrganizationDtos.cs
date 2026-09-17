using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class OrganizationSummaryDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public int TotalBranches { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalDesignations { get; set; }
    public int TotalGrades { get; set; }
    public int TotalCostCenters { get; set; }
    public int TotalShifts { get; set; }
    public int TotalHolidays { get; set; }
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
}

public class CompanyProfileDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    // Basic
    public string CompanyName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? CompanyCode { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Industry { get; set; }
    public string? CompanyType { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PinCode { get; set; }

    // Contact
    public string? PrimaryContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    // Business
    public string? Pan { get; set; }
    public string? Tan { get; set; }
    public string? Gstin { get; set; }
    public string? PfRegistrationNumber { get; set; }
    public string? EsicRegistrationNumber { get; set; }
    public string? ProfessionalTaxNumber { get; set; }

    // Branding
    public string? CompanyLogoUrl { get; set; }

    // Settings
    public string DefaultCurrency { get; set; } = "INR";
    public string Timezone { get; set; } = "India Standard Time";
    public string DateFormat { get; set; } = "DD/MM/YYYY";
    public int FinancialYearStartMonth { get; set; } = 4;
    public string PayrollCycle { get; set; } = "Monthly";

    public DateTime UpdatedAt { get; set; }
}

public class CompanyProfileUpsertDto
{
    [Required]
    [MaxLength(256)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? LegalName { get; set; }

    [MaxLength(64)]
    public string? CompanyCode { get; set; }

    [MaxLength(128)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(128)]
    public string? Industry { get; set; }

    [MaxLength(64)]
    public string? CompanyType { get; set; }

    [MaxLength(256)]
    public string? Website { get; set; }

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? AddressLine1 { get; set; }

    [MaxLength(256)]
    public string? AddressLine2 { get; set; }

    [MaxLength(128)]
    public string? City { get; set; }

    [MaxLength(128)]
    public string? State { get; set; }

    [MaxLength(128)]
    public string? Country { get; set; }

    [MaxLength(32)]
    public string? PinCode { get; set; }

    [MaxLength(128)]
    public string? PrimaryContactPerson { get; set; }

    [EmailAddress]
    [MaxLength(256)]
    public string? ContactEmail { get; set; }

    [MaxLength(32)]
    public string? ContactPhone { get; set; }

    [MaxLength(32)]
    public string? Pan { get; set; }

    [MaxLength(32)]
    public string? Tan { get; set; }

    [MaxLength(32)]
    public string? Gstin { get; set; }

    [MaxLength(64)]
    public string? PfRegistrationNumber { get; set; }

    [MaxLength(64)]
    public string? EsicRegistrationNumber { get; set; }

    [MaxLength(64)]
    public string? ProfessionalTaxNumber { get; set; }

    [MaxLength(512)]
    public string? CompanyLogoUrl { get; set; }

    [MaxLength(16)]
    public string DefaultCurrency { get; set; } = "INR";

    [MaxLength(64)]
    public string Timezone { get; set; } = "India Standard Time";

    [MaxLength(32)]
    public string DateFormat { get; set; } = "DD/MM/YYYY";

    public int FinancialYearStartMonth { get; set; } = 4;

    [MaxLength(32)]
    public string PayrollCycle { get; set; } = "Monthly";
}

public class GradeDto
{
    public int Id { get; set; }
    public string GradeCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public int Level { get; set; }
    public string? Description { get; set; }
    public decimal? MinimumSalary { get; set; }
    public decimal? MaximumSalary { get; set; }
    public bool IsActive { get; set; }
}

public class GradeUpsertDto
{
    [Required]
    [MaxLength(32)]
    public string GradeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string GradeName { get; set; } = string.Empty;

    public int Level { get; set; } = 1;

    [MaxLength(512)]
    public string? Description { get; set; }

    public decimal? MinimumSalary { get; set; }
    public decimal? MaximumSalary { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CostCenterDto
{
    public int Id { get; set; }
    public string CostCenterCode { get; set; } = string.Empty;
    public string CostCenterName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CostCenterUpsertDto
{
    [Required]
    [MaxLength(32)]
    public string CostCenterCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string CostCenterName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public int? BranchId { get; set; }
    public int? ManagerId { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ShiftDto
{
    public int Id { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public string StartTime { get; set; } = "09:00";
    public string EndTime { get; set; } = "18:00";
    public int GracePeriodMinutes { get; set; }
    public int BreakDurationMinutes { get; set; }
    public decimal WorkingHours { get; set; }
    public bool IsActive { get; set; }
}

public class ShiftUpsertDto
{
    [Required]
    [MaxLength(32)]
    public string ShiftCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ShiftName { get; set; } = string.Empty;

    public string StartTime { get; set; } = "09:00";
    public string EndTime { get; set; } = "18:00";
    public int GracePeriodMinutes { get; set; } = 15;
    public int BreakDurationMinutes { get; set; } = 60;
    public decimal WorkingHours { get; set; } = 8.0m;
    public bool IsActive { get; set; } = true;
}

public class HolidayDto
{
    public int Id { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateOnly HolidayDate { get; set; }
    public string HolidayType { get; set; } = "Public";
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public bool IsOptional { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class HolidayUpsertDto
{
    [Required]
    [MaxLength(128)]
    public string HolidayName { get; set; } = string.Empty;

    [Required]
    public DateOnly HolidayDate { get; set; }

    [MaxLength(64)]
    public string HolidayType { get; set; } = "Public"; // Company, Public, Optional

    public int? BranchId { get; set; }
    public bool IsOptional { get; set; } = false;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WorkingDayConfigDto
{
    public int Id { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int DayOfWeek { get; set; } // 1 (Mon) - 7 (Sun)
    public string DayName { get; set; } = string.Empty;
    public bool IsWorkingDay { get; set; }
    public bool IsWeeklyOff { get; set; }
}

public class WorkingDayConfigBatchUpdateDto
{
    public int? BranchId { get; set; }
    public List<WorkingDayItemDto> Days { get; set; } = new();
}

public class WorkingDayItemDto
{
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsWorkingDay { get; set; }
    public bool IsWeeklyOff { get; set; }
}
