namespace CRM.Helpers
{
    /// <summary>Named pipeline milestones used for enterprise deal stage validation.</summary>
    internal static class DealStageMilestoneRules
    {
        public const string MilestoneBlockedMessage =
            "This stage cannot be selected because a later business milestone has already been completed.";

        public const string FullPaymentReceived = "Full Payment Received";
        public const string MaterialDispatched = "Material Dispatched";
        public const string MaterialDelivered = "Material Delivered";
        public const string ProductionStarted = "Production Started";
        public const string AdvancePaymentPending = "Advance Payment Pending";
        public const string AdvancePaymentReceived = "Advance Payment Received";

        private static readonly HashSet<string> AdvancePaymentStageNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                AdvancePaymentPending,
                AdvancePaymentReceived,
            };

        public static bool HasReachedStage(
            string currentStatus,
            IEnumerable<string> historyNewStages,
            string milestoneName)
        {
            if (NameMatches(currentStatus, milestoneName))
            {
                return true;
            }

            return historyNewStages.Any(h => NameMatches(h, milestoneName));
        }

        public static bool IsAdvancePaymentStage(string statusLabel) =>
            AdvancePaymentStageNames.Contains(statusLabel.Trim());

        public static bool IsMilestoneBlockedTarget(
            string toStatus,
            string currentStatus,
            IEnumerable<string> historyNewStages,
            Func<string, int> resolveSortOrder,
            Func<string, bool> isClosedWon,
            Func<string, bool> isClosedLost)
        {
            var to = toStatus.Trim();

            if (HasReachedStage(currentStatus, historyNewStages, MaterialDelivered))
            {
                if (NameMatches(to, MaterialDelivered))
                {
                    return false;
                }

                if (isClosedWon(to) || isClosedLost(to))
                {
                    return false;
                }

                return true;
            }

            if (HasReachedStage(currentStatus, historyNewStages, MaterialDispatched))
            {
                var productionOrder = resolveSortOrder(ProductionStarted);
                var toOrder = resolveSortOrder(to);
                if (productionOrder >= 0 && toOrder >= 0 && toOrder <= productionOrder)
                {
                    return true;
                }
            }

            if (HasReachedStage(currentStatus, historyNewStages, FullPaymentReceived))
            {
                if (IsAdvancePaymentStage(to))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool NameMatches(string a, string b) =>
            string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
