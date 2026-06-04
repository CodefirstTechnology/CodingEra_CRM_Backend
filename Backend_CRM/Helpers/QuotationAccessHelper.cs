using CRM.DATA;
using CRM.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    public sealed class QuotationUserContext
    {
        public int UserId { get; init; }

        /// <summary>Admin or super-admin — may view and mutate all quotations.</summary>
        public bool CanViewAll { get; init; }
    }

    public static class QuotationAccessHelper
    {
        public static async Task<(QuotationUserContext? Context, IActionResult? Error)> ResolveUserContextAsync(
            TaskDbcontext db,
            int userId,
            HttpRequest? request = null)
        {
            if (request != null)
            {
                var bindErr = ApiActingUserValidation.EnsureQueryUserMatchesBearer(request, userId);
                if (bindErr != null)
                {
                    return (null, bindErr);
                }
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(db, userId);
            if (auditErr != null)
            {
                return (null, auditErr);
            }

            var canViewAll = await CrmRoleHelper.CanViewAllRecordsAsync(db, userId);
            return (new QuotationUserContext { UserId = userId, CanViewAll = canViewAll }, null);
        }

        public static IQueryable<Quotation> ApplyVisibilityFilter(
            IQueryable<Quotation> query,
            QuotationUserContext context)
        {
            if (context.CanViewAll)
            {
                return query;
            }

            return query.Where(q => q.CreatedBy == context.UserId);
        }

        public static IQueryable<Quotation> ApplySearchFilter(IQueryable<Quotation> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return query;
            }

            var term = search.Trim().ToLower();
            return query.Where(q =>
                (q.QuotationNumber != null && q.QuotationNumber.ToLower().Contains(term))
                || (q.CustomerName != null && q.CustomerName.ToLower().Contains(term))
                || (q.CompanyName != null && q.CompanyName.ToLower().Contains(term))
                || (q.EmailAddress != null && q.EmailAddress.ToLower().Contains(term))
                || (q.ReferenceNumber != null && q.ReferenceNumber.ToLower().Contains(term)));
        }

        /// <summary>Returns NotFound when the quotation is missing or not visible (avoids leaking ids).</summary>
        public static async Task<IActionResult?> EnsureCanAccessAsync(
            TaskDbcontext db,
            int quotationId,
            QuotationUserContext context)
        {
            var row = await db.Quotations.AsNoTracking()
                .Where(q => q.Id == quotationId)
                .Select(q => new { q.CreatedBy })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return new NotFoundResult();
            }

            if (context.CanViewAll || row.CreatedBy == context.UserId)
            {
                return null;
            }

            return new NotFoundResult();
        }
    }
}
