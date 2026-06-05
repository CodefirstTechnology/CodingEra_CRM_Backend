using CRM.DATA;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    public static class QuotationDealLockHelper
    {
        public const string ClosedDealMessage =
            "Quotations linked to closed deals cannot be modified.";

        public static async Task<IReadOnlyList<DealStatus>> LoadActivePipelineAsync(TaskDbcontext context) =>
            DealStageValidationHelper.OrderPipeline(
                await context.DealStatuses.AsNoTracking().ToListAsync());

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
            return pipeline.Count > 0 && DealStageValidationHelper.IsClosed(status, pipeline);
        }
    }
}
