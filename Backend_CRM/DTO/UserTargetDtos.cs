namespace CRM.DTO
{
    public class UserTargetTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserTargetSalesUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class UserTargetDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int TargetTypeId { get; set; }
        public string TargetTypeName { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public bool IsActive { get; set; }
        public decimal AchievedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal AchievementPercent { get; set; }
        public DateTime? AchievedCalculatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UserTargetUpsertDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TargetTypeId { get; set; }
        public decimal TargetAmount { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UserTargetStatusPatchDto
    {
        public bool IsActive { get; set; }
    }

    public class UserTargetMonitorQueryDto
    {
        public string? Search { get; set; }
        public int? UserId { get; set; }
        public int? TargetTypeId { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; }
        public string? SortDir { get; set; }
    }

    public class UserTargetWidgetDto
    {
        public int TargetId { get; set; }
        public string TargetTypeName { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal AchievedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal AchievementPercent { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
