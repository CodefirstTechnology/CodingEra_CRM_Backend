using CRM.DATA;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    public static class QuotationDealLockHelper
    {
        public const string ClosedDealMessage =
            "Quotations linked to delivered or closed deals cannot be modified.";

        public const string GenerationBlockedMessage =
            "Quotations cannot be created when the deal is Material Delivered or Lead Closed - Lost.";

        public static async Task<IReadOnlyList<DealStatus>> LoadActivePipelineAsync(TaskDbcontext context) =>
            DealStageValidationHelper.OrderPipeline(
                await context.DealStatuses.AsNoTracking().ToListAsync());

        public static bool IsQuotationGenerationBlocked(
            string status,
            IReadOnlyList<DealStatus> allStatuses)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            var normalized = status.Trim();
            if (DealStageValidationHelper.IsClosedLost(normalized, allStatuses))
            {
                return true;
            }

            return string.Equals(
                normalized,
                DealStageMilestoneRules.MaterialDelivered,
                StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<bool> IsQuotationGenerationBlockedAsync(
            TaskDbcontext context,
            int? dealId)
        {
            if (dealId is not > 0)
            {
                return false;
            }

            var status = await context.Deals.AsNoTracking()
                .Where(d => d.Id == dealId)
                .Select(d => d.Status)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            var allStatuses = await context.DealStatuses.AsNoTracking().ToListAsync();
            return IsQuotationGenerationBlocked(status, allStatuses);
        }

        public static async Task<bool> IsDealClosedAsync(
            TaskDbcontext context,
            int? dealId,
            IReadOnlyList<DealStatus>? pipeline = null)
        {
            if (dealId is not > 0)
            {
                return false;
            }

            var status = await context.Deals.AsNoTracking()
                .Where(d => d.Id == dealId)
                .Select(d => d.Status)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            pipeline ??= await LoadActivePipelineAsync(context);
            return pipeline.Count > 0 && DealStageValidationHelper.IsDealDataLocked(status, pipeline);
        }

        public static async Task SyncDealAmountFromGrandTotalAsync(
            TaskDbcontext context,
            int? dealId,
            decimal grandTotal)
        {
            if (dealId is not > 0)
            {
                return;
            }

            var deal = await context.Deals.FirstOrDefaultAsync(d => d.Id == dealId);
            if (deal == null)
            {
                return;
            }

            deal.DealAmount = grandTotal;
        }
    }
}
