using CRM.DATA;
using CRM.models;
using CRM.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Shared lead list/export filters (status, source, owner, search, LeadDate range).</summary>
    public static class LeadQueryFilterHelper
    {
        public static IQueryable<Lead> QueryWithMasters(IQueryable<Lead> q) =>
            q.Include(l => l.Salutation)
                .Include(l => l.LeadStatus)
                .Include(l => l.RequestType)
                .Include(l => l.LeadOwner)
                .Include(l => l.Organization)
                .ThenInclude(o => o!.Industry)
                .Include(l => l.Organization)
                .ThenInclude(o => o!.EmployeeCount)
                .Include(l => l.Organization)
                .ThenInclude(o => o!.Territory);

        public static async Task<IQueryable<Lead>> ApplyListFiltersAsync(
            TaskDbcontext db,
            IRbacService rbac,
            int userId,
            IQueryable<Lead> query,
            string? leadSource,
            string? status,
            int? leadOwnerId,
            string? search = null,
            DateOnly? leadDateFrom = null,
            DateOnly? leadDateTo = null,
            CancellationToken cancellationToken = default)
        {
            if (leadOwnerId is > 0 && await rbac.IsAdminUserAsync(userId))
            {
                query = query.Where(l => l.LeadOwnerId == leadOwnerId);
            }

            if (!string.IsNullOrWhiteSpace(leadSource))
            {
                var src = leadSource.Trim();
                query = query.Where(l => l.LeadSource == src);
            }

            query = await ApplyStatusFilterAsync(db, query, status, cancellationToken);

            if (leadDateFrom != null || leadDateTo != null)
            {
                // LeadDate only — null LeadDate rows are excluded when a range is active.
                if (leadDateFrom != null && leadDateTo != null)
                {
                    var fromDt = leadDateFrom.Value.ToDateTime(TimeOnly.MinValue);
                    var toDt = leadDateTo.Value.ToDateTime(TimeOnly.MinValue);
                    query = query.Where(l =>
                        l.LeadDate != null &&
                        l.LeadDate.Value.Date >= fromDt &&
                        l.LeadDate.Value.Date <= toDt);
                }
                else if (leadDateFrom != null)
                {
                    var fromDt = leadDateFrom.Value.ToDateTime(TimeOnly.MinValue);
                    query = query.Where(l => l.LeadDate != null && l.LeadDate.Value.Date >= fromDt);
                }
                else
                {
                    var toDt = leadDateTo!.Value.ToDateTime(TimeOnly.MinValue);
                    query = query.Where(l => l.LeadDate != null && l.LeadDate.Value.Date <= toDt);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLowerInvariant();
                query = query.Where(l =>
                    (l.FirstName != null && l.FirstName.ToLower().Contains(q)) ||
                    (l.LastName != null && l.LastName.ToLower().Contains(q)) ||
                    (l.Email != null && l.Email.ToLower().Contains(q)) ||
                    (l.Mobile != null && l.Mobile.ToLower().Contains(q)) ||
                    (l.Notes != null && l.Notes.ToLower().Contains(q)) ||
                    (l.LeadSource != null && l.LeadSource.ToLower().Contains(q)) ||
                    (l.Organization != null && l.Organization.Name != null &&
                     l.Organization.Name.ToLower().Contains(q)) ||
                    (l.LeadOwner != null && l.LeadOwner.FullName != null &&
                     l.LeadOwner.FullName.ToLower().Contains(q)) ||
                    (l.Organization != null && l.Organization.Industry != null &&
                     l.Organization.Industry.Name != null &&
                     l.Organization.Industry.Name.ToLower().Contains(q)));
            }

            return query;
        }

        private static async Task<IQueryable<Lead>> ApplyStatusFilterAsync(
            TaskDbcontext db,
            IQueryable<Lead> query,
            string? status,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return query;
            }

            if (int.TryParse(status, out var statusId))
            {
                return query.Where(l => l.LeadStatusId == statusId);
            }

            var st = status.Trim();
            var names = (await LeadStatusMovedToDealSeed.ConversionStatusLookupNamesAsync(db, st))
                .Select(n => n.ToLowerInvariant())
                .ToList();

            var matchConversionFlag =
                LeadStatusMovedToDealSeed.IsConversionStatusName(st) ||
                await db.LeadStatuses.AsNoTracking().AnyAsync(ls =>
                    ls.IsConversionStatus && ls.Name.ToLower() == st.ToLower(), cancellationToken);

            return query.Where(l =>
                db.LeadStatuses.Any(ls =>
                    ls.Id == l.LeadStatusId &&
                    (names.Contains(ls.Name.ToLower()) ||
                     (matchConversionFlag && ls.IsConversionStatus))));
        }
    }
}
