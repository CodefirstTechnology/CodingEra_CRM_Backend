using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;

namespace ERP.Infrastructure.Sales
{
    internal static class SalesTargetMapper
    {
        // Delegated to shared DateHelper — kept for backward-compat callsites in this class
        private static string FormatDate(DateOnly date) => DateHelper.FormatDate(date);
        private static string FormatDateTime(DateTimeOffset value) => DateHelper.FormatDateTime(value);
        public static DateOnly ParseDate(string value, DateOnly fallback) => DateHelper.ParseDate(value, fallback);

        public static DateOnly? ParseOptionalDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (DateOnly.TryParse(value, out var date)) return date;
            if (DateTimeOffset.TryParse(value, out var dto)) return DateOnly.FromDateTime(dto.UtcDateTime);
            return null;
        }

        public static SalesTargetListItemDto ToListItem(SalesTarget e) => new()
        {
            Id = e.Id,
            TargetNumber = e.TargetNumber,
            TargetName = e.TargetName,
            TargetType = e.TargetType,
            TargetCategory = e.TargetCategory,
            AssignmentType = e.AssignmentType,
            SalesPersonUserId = e.SalesPersonUserId,
            SalesTeam = e.SalesTeam,
            Branch = e.Branch,
            FinancialYear = e.FinancialYear,
            StartDate = FormatDate(e.StartDate),
            EndDate = FormatDate(e.EndDate),
            TargetValue = e.TargetValue,
            AchievedValue = e.AchievedValue,
            RemainingValue = e.RemainingValue,
            OverAchievementValue = e.OverAchievementValue,
            AchievementPercentage = e.AchievementPercentage,
            IsLocked = e.IsLocked,
            Currency = e.Currency,
            Status = e.Status
        };

        public static SalesTargetRealizationDto ToRealizationDto(SalesTargetRealization r) => new()
        {
            Id = r.Id,
            SalesTargetId = r.SalesTargetId,
            SalesOrderId = r.SalesOrderId,
            InvoiceId = r.InvoiceId,
            TransactionType = r.TransactionType,
            RealizedAmount = r.RealizedAmount,
            IsReversal = r.IsReversal,
            AppliedOn = FormatDateTime(r.AppliedOn),
            AppliedBy = r.AppliedBy
        };

        public static SalesTargetAssignmentDto ToAssignmentDto(SalesTargetAssignment a) => new()
        {
            Id = a.Id,
            SalesPersonUserId = a.SalesPersonUserId,
            AssignedTarget = a.AssignedTarget,
            AchievedValue = a.AchievedValue,
            AchievementPercentage = a.AchievementPercentage,
            Status = a.Status,
            Remarks = a.Remarks,
            AssignedBy = a.AssignedBy,
            AssignedDate = FormatDateTime(a.AssignedDate)
        };

        public static SalesTargetProgressDto ToProgressDto(SalesTargetProgressHistory p) => new()
        {
            Id = p.Id,
            OldAchievedValue = p.OldAchievedValue,
            NewAchievedValue = p.NewAchievedValue,
            AchievementPercentage = p.AchievementPercentage,
            Action = p.Action,
            Remarks = p.Remarks,
            UpdatedBy = p.UpdatedBy,
            UpdatedOn = FormatDateTime(p.UpdatedOn)
        };

        public static SalesTargetHistoryDto ToHistoryDto(SalesTargetStatusHistory h) => new()
        {
            Id = h.Id,
            OldStatus = h.OldStatus,
            NewStatus = h.NewStatus,
            Remarks = h.Remarks,
            ChangedBy = h.ChangedBy,
            ChangedOn = FormatDateTime(h.ChangedOn)
        };

        public static SalesTargetDto ToDto(SalesTarget e) => new()
        {
            Id = e.Id,
            TargetNumber = e.TargetNumber,
            TargetName = e.TargetName,
            TargetType = e.TargetType,
            TargetCategory = e.TargetCategory,
            AssignmentType = e.AssignmentType,
            SalesPersonUserId = e.SalesPersonUserId,
            SalesTeam = e.SalesTeam,
            Branch = e.Branch,
            RegionalManager = e.RegionalManager,
            FinancialYear = e.FinancialYear,
            StartDate = FormatDate(e.StartDate),
            EndDate = FormatDate(e.EndDate),
            TargetValue = e.TargetValue,
            AchievedValue = e.AchievedValue,
            RemainingValue = e.RemainingValue,
            OverAchievementValue = e.OverAchievementValue,
            AchievementPercentage = e.AchievementPercentage,
            IsLocked = e.IsLocked,
            RevisionNumber = e.RevisionNumber,
            ParentTargetId = e.ParentTargetId,
            IsAutoAggregated = e.IsAutoAggregated,
            OriginalTargetValue = e.OriginalTargetValue,
            ProrationFactor = e.ProrationFactor,
            CalculatedCommissionAmount = e.CalculatedCommissionAmount,
            Forecast = SalesTargetAnalyticsService.ComputeForecast(e, DateOnly.FromDateTime(DateTime.UtcNow)),
            ChildTargets = e.ChildTargets?.Where(c => !c.IsDeleted).Select(ToListItem).ToList() ?? new(),
            Currency = e.Currency,
            Status = e.Status,
            Remarks = e.Remarks,
            CreatedBy = e.CreatedBy,
            CreatedDate = FormatDateTime(e.CreatedDate),
            UpdatedBy = e.UpdatedBy,
            UpdatedDate = FormatDateTime(e.UpdatedDate),
            Assignments = e.Assignments.OrderBy(a => a.AssignedDate).Select(ToAssignmentDto).ToList(),
            ProgressHistory = e.ProgressHistory.OrderBy(p => p.UpdatedOn).Select(ToProgressDto).ToList(),
            StatusHistory = e.StatusHistory.OrderBy(h => h.ChangedOn).Select(ToHistoryDto).ToList(),
            Realizations = e.Realizations.OrderByDescending(r => r.AppliedOn).Select(ToRealizationDto).ToList()
        };
    }
}
