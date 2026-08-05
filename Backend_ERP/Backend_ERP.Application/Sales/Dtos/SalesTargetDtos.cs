namespace ERP.Application.Sales.Dtos
{
    public class SalesTargetDto
    {
        public int Id { get; set; }
        public string TargetNumber { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetCategory { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public int? SalesPersonUserId { get; set; }
        public string SalesTeam { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string RegionalManager { get; set; } = string.Empty;
        public int FinancialYear { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public decimal AchievedValue { get; set; }
        public decimal RemainingValue { get; set; }
        public decimal AchievementPercentage { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedDate { get; set; } = string.Empty;
        public List<SalesTargetAssignmentDto> Assignments { get; set; } = new();
        public List<SalesTargetProgressDto> ProgressHistory { get; set; } = new();
        public List<SalesTargetHistoryDto> StatusHistory { get; set; } = new();
    }

    public class SalesTargetListItemDto
    {
        public int Id { get; set; }
        public string TargetNumber { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetCategory { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public int? SalesPersonUserId { get; set; }
        public string SalesTeam { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public int FinancialYear { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public decimal AchievedValue { get; set; }
        public decimal RemainingValue { get; set; }
        public decimal AchievementPercentage { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class SalesTargetAssignmentDto
    {
        public int Id { get; set; }
        public int SalesPersonUserId { get; set; }
        public decimal AssignedTarget { get; set; }
        public decimal AchievedValue { get; set; }
        public decimal AchievementPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = string.Empty;
        public string AssignedDate { get; set; } = string.Empty;
    }

    public class SalesTargetProgressDto
    {
        public int Id { get; set; }
        public decimal OldAchievedValue { get; set; }
        public decimal NewAchievedValue { get; set; }
        public decimal AchievementPercentage { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedOn { get; set; } = string.Empty;
    }

    public class SalesTargetHistoryDto
    {
        public int Id { get; set; }
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public string ChangedOn { get; set; } = string.Empty;
    }

    public class SalesTargetCreateRequestDto
    {
        public string? TargetNumber { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetCategory { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public int? SalesPersonUserId { get; set; }
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public string? RegionalManager { get; set; }
        public int FinancialYear { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public decimal? AchievedValue { get; set; }
        public string Currency { get; set; } = "INR";
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public List<SalesTargetAssignmentRequestDto>? Assignments { get; set; }
    }

    public class SalesTargetUpdateRequestDto
    {
        public string? TargetNumber { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetCategory { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public int? SalesPersonUserId { get; set; }
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public string? RegionalManager { get; set; }
        public int FinancialYear { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public string Currency { get; set; } = "INR";
        public string? Remarks { get; set; }
        public List<SalesTargetAssignmentRequestDto>? Assignments { get; set; }
    }

    public class SalesTargetAssignmentRequestDto
    {
        public int SalesPersonUserId { get; set; }
        public decimal AssignedTarget { get; set; }
        public decimal? AchievedValue { get; set; }
        public string? Remarks { get; set; }
    }

    public class SalesTargetDuplicateRequestDto
    {
        public string? TargetName { get; set; }
        public int? FinancialYear { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public decimal? TargetValue { get; set; }
    }

    public class SalesTargetCopyPreviousRequestDto
    {
        public int? SourceSalesPersonUserId { get; set; }
        public int? SourceFinancialYear { get; set; }
        public string? TargetCategory { get; set; }
        public int TargetFinancialYear { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string? TargetName { get; set; }
    }

    public class SalesTargetStatusUpdateRequestDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class SalesTargetProgressUpdateRequestDto
    {
        public decimal AchievedValue { get; set; }
        public string? Remarks { get; set; }
    }

    public class SalesTargetRemarksRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class SalesTargetListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? TargetType { get; set; }
        public string? TargetCategory { get; set; }
        public int? SalesPersonUserId { get; set; }
        public int? FinancialYear { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class SalesTargetDashboardDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
        public int ExpiredCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalTargetValue { get; set; }
        public decimal TotalAchievedValue { get; set; }
        public decimal AverageAchievementPercentage { get; set; }
        public List<SalesTargetListItemDto> Recent { get; set; } = new();
    }

    public class SalesTargetReportDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public decimal TotalTargetValue { get; set; }
        public decimal TotalAchievedValue { get; set; }
        public List<SalesTargetListItemDto> Rows { get; set; } = new();
    }

    public class SalesTargetExportRequestDto
    {
        public string Format { get; set; } = "csv";
        public string? Status { get; set; }
        public string? TargetCategory { get; set; }
        public int? FinancialYear { get; set; }
    }

    public class SalesTargetExportMetadataDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string GeneratedOn { get; set; } = string.Empty;
    }

    public class SalesTargetLookupDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
