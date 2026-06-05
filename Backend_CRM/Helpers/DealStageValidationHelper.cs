using CRM.models;

namespace CRM.Helpers
{
    public sealed class DealStageValidationResult
    {
        public bool Allowed { get; init; }
        public string? Message { get; init; }

        public static DealStageValidationResult Ok() => new() { Allowed = true };

        public static DealStageValidationResult Fail(string message) => new() { Allowed = false, Message = message };
    }

    /// <summary>Enterprise deal pipeline validation driven by <see cref="DealStatus"/> master rows.</summary>
    public static class DealStageValidationHelper
    {
        public const string ClosedDealMessage = "Closed deals cannot be modified.";

        public const string WonRequiresMaterialDeliveredMessage =
            "Closed Won is allowed only after Material Delivered.";

        public const string LostReasonRequiredMessage =
            "Lost reason is required when closing a deal as lost.";

        public static IReadOnlyList<DealStatus> OrderPipeline(IEnumerable<DealStatus> rows, bool activeOnly = true) =>
            rows.Where(s => !activeOnly || s.IsActive)
                .OrderBy(s => s.SortOrder > 0 ? s.SortOrder : s.Id * 10)
                .ThenBy(s => s.Id)
                .ToList();

        public static bool IsClosedWon(string status, IReadOnlyList<DealStatus> allStatuses) =>
            ResolveRow(allStatuses, status)?.IsWon == true;

        public static bool IsClosedLost(string status, IReadOnlyList<DealStatus> allStatuses) =>
            ResolveRow(allStatuses, status)?.IsLost == true;

        public static bool IsClosed(string status, IReadOnlyList<DealStatus> allStatuses) =>
            IsClosedWon(status, allStatuses) || IsClosedLost(status, allStatuses);

        public static DealStageValidationResult ValidateTransition(
            IReadOnlyList<DealStatus> allStatuses,
            IReadOnlyList<DealStatus> activePipeline,
            string fromStatus,
            string toStatus,
            IEnumerable<string> historyNewStages,
            string? lostReason = null)
        {
            if (activePipeline.Count == 0)
            {
                return DealStageValidationResult.Ok();
            }

            var from = fromStatus.Trim();
            var to = toStatus.Trim();
            var history = historyNewStages.ToList();

            if (IsClosed(from, allStatuses))
            {
                return DealStageValidationResult.Fail(ClosedDealMessage);
            }

            var fromOrder = ResolveSortOrder(allStatuses, from);
            var toOrder = ResolveSortOrder(allStatuses, to);
            if (fromOrder >= 0 && fromOrder == toOrder)
            {
                return DealStageValidationResult.Ok();
            }

            if (IsClosedLost(to, allStatuses))
            {
                if (string.IsNullOrWhiteSpace(lostReason))
                {
                    return DealStageValidationResult.Fail(LostReasonRequiredMessage);
                }

                return DealStageValidationResult.Ok();
            }

            if (IsClosedWon(to, allStatuses))
            {
                if (!DealStageMilestoneRules.HasReachedStage(
                        from,
                        history,
                        DealStageMilestoneRules.MaterialDelivered))
                {
                    return DealStageValidationResult.Fail(WonRequiresMaterialDeliveredMessage);
                }

                return DealStageValidationResult.Ok();
            }

            if (DealStageMilestoneRules.IsMilestoneBlockedTarget(
                    to,
                    from,
                    history,
                    name => ResolveSortOrder(allStatuses, name),
                    name => IsClosedWon(name, allStatuses),
                    name => IsClosedLost(name, allStatuses)))
            {
                return DealStageValidationResult.Fail(DealStageMilestoneRules.MilestoneBlockedMessage);
            }

            return DealStageValidationResult.Ok();
        }

        private static DealStatus? ResolveRow(IReadOnlyList<DealStatus> allStatuses, string statusLabel)
        {
            var normalized = statusLabel.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            return allStatuses.FirstOrDefault(s =>
                string.Equals(s.Name, normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static int ResolveSortOrder(IReadOnlyList<DealStatus> allStatuses, string statusLabel)
        {
            var row = ResolveRow(allStatuses, statusLabel);
            if (row == null)
            {
                return -1;
            }

            return row.SortOrder > 0 ? row.SortOrder : row.Id * 10;
        }
    }
}
