using System;
using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public class SalesTarget
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public string TargetNumber { get; set; } = string.Empty;

        public string TargetName { get; set; } = string.Empty;

        // Temporal & Period Dimensions
        public string TargetPeriod { get; set; } = string.Empty; // e.g., "2026-M04", "2026-Q1", "FY2026"

        public int FiscalYear { get; set; } = 2026;

        public int FinancialYear
        {
            get => FiscalYear;
            set => FiscalYear = value;
        }

        public string PeriodType { get; set; } = SalesTargetTypes.Monthly;

        public string TargetType
        {
            get => PeriodType;
            set => PeriodType = value;
        }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        // Scope & Hierarchy Dimensions
        public int? TerritoryId { get; set; }

        public string Branch { get; set; } = string.Empty;

        public string SalesTeam { get; set; } = string.Empty;

        public string RegionalManager { get; set; } = string.Empty;

        public int? SalesPersonUserId { get; set; }

        public string AssignmentType { get; set; } = SalesTargetAssignmentTypes.IndividualSalesperson;

        public int? ParentTargetId { get; set; }

        public SalesTarget? ParentTarget { get; set; }

        public ICollection<SalesTarget> ChildTargets { get; set; } = new List<SalesTarget>();

        public bool IsAutoAggregated { get; set; } = false;

        // Metric Quota Commitments
        public string TargetCategory { get; set; } = SalesTargetCategories.Revenue;

        public decimal RevenueTarget { get; set; }

        public decimal TargetValue
        {
            get => RevenueTarget;
            set => RevenueTarget = value;
        }

        public decimal VolumeTarget { get; set; } = 0m;

        public decimal AchievedVolume { get; set; } = 0m;

        public decimal MarginHurdleTarget { get; set; } = 0m;

        public decimal RealizedMarginPercentage { get; set; } = 0m;

        public decimal RealizedMarginAmount { get; set; } = 0m;

        // Multi-Stream Realization Accumulators (Synchronized via DB Trigger & Realization Engine)
        public decimal RealizedRevenue { get; set; } = 0m;

        public decimal RealizedCash { get; set; } = 0m;

        public decimal RealizedBilled { get; set; } = 0m;

        public decimal AchievedValue { get; set; } = 0m;

        public decimal RemainingValue { get; set; } = 0m;

        public decimal OverAchievementValue { get; set; } = 0m;

        public decimal AchievementPercentage { get; set; } = 0m;

        // Governance, Revisions & Currency Lock
        public string Currency { get; set; } = "INR";

        public decimal LockedExchangeRate { get; set; } = 1.000000m;

        public string Status { get; set; } = SalesTargetStatuses.Draft;

        public bool IsLocked { get; set; } = false;

        public int RevisionNumber { get; set; } = 1;

        public int? PredecessorTargetId { get; set; }

        public string? ApprovalToken { get; set; }

        public decimal? OriginalTargetValue { get; set; }

        public decimal ProrationFactor { get; set; } = 1.0m;

        public decimal CalculatedCommissionAmount { get; set; } = 0m;

        public string Remarks { get; set; } = string.Empty;

        // Optimistic Concurrency Token via PostgreSQL system column 'xmin'
        public uint ConcurrencyVersion { get; set; }

        // Audit Trail & Soft Delete
        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; } = DateTimeOffset.UtcNow;

        public bool IsDeleted { get; set; } = false;

        // Navigation Collections
        public ICollection<SalesTargetItem> Items { get; set; } = new List<SalesTargetItem>();

        public ICollection<SalesTargetAssignment> Assignments { get; set; } =
            new List<SalesTargetAssignment>();

        public ICollection<SalesTargetProgressHistory> ProgressHistory { get; set; } =
            new List<SalesTargetProgressHistory>();

        public ICollection<SalesTargetStatusHistory> StatusHistory { get; set; } =
            new List<SalesTargetStatusHistory>();

        public ICollection<SalesTargetRealization> Realizations { get; set; } =
            new List<SalesTargetRealization>();

        public ICollection<SalesTargetApprovalHistory> ApprovalHistories { get; set; } =
            new List<SalesTargetApprovalHistory>();
    }
}
