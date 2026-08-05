namespace ERP.Domain.Sales
{
    public class SalesTarget
    {
        public int Id { get; set; }

        public string TargetNumber { get; set; } = string.Empty;

        public string TargetName { get; set; } = string.Empty;

        public string TargetType { get; set; } = SalesTargetTypes.Monthly;

        public string TargetCategory { get; set; } = SalesTargetCategories.Revenue;

        public string AssignmentType { get; set; } = SalesTargetAssignmentTypes.IndividualSalesperson;

        public int? SalesPersonUserId { get; set; }

        public string SalesTeam { get; set; } = string.Empty;

        public string Branch { get; set; } = string.Empty;

        public string RegionalManager { get; set; } = string.Empty;

        public int FinancialYear { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public decimal TargetValue { get; set; }

        public decimal AchievedValue { get; set; }

        public decimal RemainingValue { get; set; }

        public decimal AchievementPercentage { get; set; }

        public string Currency { get; set; } = "INR";

        public string Status { get; set; } = SalesTargetStatuses.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<SalesTargetAssignment> Assignments { get; set; } =
            new List<SalesTargetAssignment>();

        public ICollection<SalesTargetProgressHistory> ProgressHistory { get; set; } =
            new List<SalesTargetProgressHistory>();

        public ICollection<SalesTargetStatusHistory> StatusHistory { get; set; } =
            new List<SalesTargetStatusHistory>();
    }
}
