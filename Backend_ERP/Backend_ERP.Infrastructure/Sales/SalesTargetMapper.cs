using System;
using System.Linq;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;

namespace ERP.Infrastructure.Sales
{
    internal static class SalesTargetMapper
    {
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
            TargetPeriod = e.TargetPeriod,
            PeriodType = e.PeriodType,
            TargetCategory = e.TargetCategory,
            AssignmentType = e.AssignmentType,
            SalesPersonUserId = e.SalesPersonUserId,
            SalesTeam = e.SalesTeam,
            Branch = e.Branch,
            FinancialYear = e.FiscalYear,
            StartDate = FormatDate(e.StartDate),
            EndDate = FormatDate(e.EndDate),
            RevenueTarget = e.RevenueTarget,
            VolumeTarget = e.VolumeTarget,
            AchievedVolume = e.AchievedVolume,
            MarginHurdleTarget = e.MarginHurdleTarget,
            RealizedRevenue = e.RealizedRevenue,
            RealizedCash = e.RealizedCash,
            RealizedBilled = e.RealizedBilled,
            AchievedValue = e.AchievedValue,
            RemainingValue = e.RemainingValue,
            OverAchievementValue = e.OverAchievementValue,
            AchievementPercentage = e.AchievementPercentage,
            IsLocked = e.IsLocked,
            RevisionNumber = e.RevisionNumber,
            Currency = e.Currency,
            Status = e.Status,
            ParentTargetId = e.ParentTargetId
        };

        public static SalesTargetRealizationDto ToRealizationDto(SalesTargetRealization r) => new()
        {
            Id = r.Id,
            SalesTargetId = r.SalesTargetId,
            SalesOrderId = r.SalesOrderId,
            AdvancePaymentId = r.AdvancePaymentId,
            SalesInvoiceId = r.SalesInvoiceId,
            QuotationId = r.QuotationId,
            RealizationType = r.RealizationType,
            RealizedAmount = r.RealizedAmount,
            RealizedQuantity = r.RealizedQuantity,
            MarginContributionAmount = r.MarginContributionAmount,
            SourceCurrency = r.SourceCurrency,
            SourceAmount = r.SourceAmount,
            LockedFXRate = r.LockedFXRate,
            IsReversal = r.IsReversal,
            ReversalOfRealizationId = r.ReversalOfRealizationId,
            ReversalReason = r.ReversalReason,
            TransactionDate = FormatDateTime(r.TransactionDate),
            AppliedOn = FormatDateTime(r.AppliedOn),
            AppliedBy = r.AppliedBy
        };

        public static SalesTargetItemDto ToItemDto(SalesTargetItem i) => new()
        {
            Id = i.Id,
            SalesTargetId = i.SalesTargetId,
            ProductCategoryId = i.ProductCategoryId,
            CategoryName = i.CategoryName,
            ItemMasterId = i.ItemMasterId,
            ItemCode = i.ItemCode,
            ItemDescription = i.ItemDescription,
            TargetQuantity = i.TargetQuantity,
            TargetRate = i.TargetRate,
            TargetValue = i.TargetValue,
            RealizedQuantity = i.RealizedQuantity,
            RealizedValue = i.RealizedValue,
            MinimumMarginPercentage = i.MinimumMarginPercentage
        };

        public static SalesTargetApprovalHistoryDto ToApprovalHistoryDto(SalesTargetApprovalHistory h) => new()
        {
            Id = h.Id,
            SalesTargetId = h.SalesTargetId,
            Action = h.Action,
            ApproverUserId = h.ApproverUserId,
            ApproverName = h.ApproverName,
            ApproverRole = h.ApproverRole,
            Comments = h.Comments,
            HmacSignatureToken = h.HmacSignatureToken,
            Timestamp = FormatDateTime(h.Timestamp)
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
            TargetPeriod = e.TargetPeriod,
            FiscalYear = e.FiscalYear,
            PeriodType = e.PeriodType,
            TargetCategory = e.TargetCategory,
            AssignmentType = e.AssignmentType,
            SalesPersonUserId = e.SalesPersonUserId,
            SalesTeam = e.SalesTeam,
            Branch = e.Branch,
            RegionalManager = e.RegionalManager,
            TerritoryId = e.TerritoryId,
            StartDate = FormatDate(e.StartDate),
            EndDate = FormatDate(e.EndDate),
            RevenueTarget = e.RevenueTarget,
            VolumeTarget = e.VolumeTarget,
            AchievedVolume = e.AchievedVolume,
            MarginHurdleTarget = e.MarginHurdleTarget,
            RealizedRevenue = e.RealizedRevenue,
            RealizedCash = e.RealizedCash,
            RealizedBilled = e.RealizedBilled,
            RealizedMarginPercentage = e.RealizedMarginPercentage,
            AchievedValue = e.AchievedValue,
            RemainingValue = e.RemainingValue,
            OverAchievementValue = e.OverAchievementValue,
            AchievementPercentage = e.AchievementPercentage,
            IsLocked = e.IsLocked,
            RevisionNumber = e.RevisionNumber,
            PredecessorTargetId = e.PredecessorTargetId,
            ApprovalToken = e.ApprovalToken,
            Currency = e.Currency,
            LockedExchangeRate = e.LockedExchangeRate,
            ParentTargetId = e.ParentTargetId,
            IsAutoAggregated = e.IsAutoAggregated,
            OriginalTargetValue = e.OriginalTargetValue,
            ProrationFactor = e.ProrationFactor,
            CalculatedCommissionAmount = e.CalculatedCommissionAmount,
            Forecast = SalesTargetAnalyticsService.ComputeForecast(e, DateOnly.FromDateTime(DateTime.UtcNow)),
            ChildTargets = e.ChildTargets?.Where(c => !c.IsDeleted).Select(ToListItem).ToList() ?? new(),
            Items = e.Items?.Select(ToItemDto).ToList() ?? new(),
            Status = e.Status,
            Remarks = e.Remarks,
            CreatedBy = e.CreatedBy,
            CreatedDate = FormatDateTime(e.CreatedDate),
            UpdatedBy = e.UpdatedBy,
            UpdatedDate = FormatDateTime(e.UpdatedDate),
            Assignments = e.Assignments?.OrderBy(a => a.AssignedDate).Select(ToAssignmentDto).ToList() ?? new(),
            ProgressHistory = e.ProgressHistory?.OrderBy(p => p.UpdatedOn).Select(ToProgressDto).ToList() ?? new(),
            StatusHistory = e.StatusHistory?.OrderBy(h => h.ChangedOn).Select(ToHistoryDto).ToList() ?? new(),
            Realizations = e.Realizations?.OrderByDescending(r => r.AppliedOn).Select(ToRealizationDto).ToList() ?? new(),
            ApprovalHistories = e.ApprovalHistories?.OrderByDescending(h => h.Timestamp).Select(ToApprovalHistoryDto).ToList() ?? new()
        };
    }
}
