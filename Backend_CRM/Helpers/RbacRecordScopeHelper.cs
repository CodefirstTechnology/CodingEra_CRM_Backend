using CRM.DATA;
using CRM.models;
using CRM.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Applies RBAC module view scope (Own / Team / All) to auditable entities.</summary>
    public static class RbacRecordScopeHelper
    {
        public static async Task<IQueryable<T>> ApplyCreatedByScopeAsync<T>(
            TaskDbcontext db,
            IRbacService rbac,
            int userId,
            string module,
            IQueryable<T> query)
            where T : class, IAuditableByUser
        {
            if (await rbac.IsAdminUserAsync(userId))
            {
                return query;
            }

            var scope = await rbac.GetModuleAccessScopeAsync(userId, module);
            if (scope is null or AccessScope.All)
            {
                return query;
            }

            if (scope == AccessScope.Own)
            {
                return query.Where(e => e.CreatedBy == userId);
            }

            if (scope == AccessScope.Team)
            {
                var roleId = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.RoleId)
                    .FirstOrDefaultAsync();

                if (roleId == null)
                {
                    return query.Where(e => e.CreatedBy == userId);
                }

                var teamUserIds = db.Users.AsNoTracking()
                    .Where(u => u.IsActive && u.RoleId == roleId)
                    .Select(u => u.Id);

                return query.Where(e => e.CreatedBy != null && teamUserIds.Contains(e.CreatedBy.Value));
            }

            return query.Where(e => e.CreatedBy == userId);
        }

        public static async Task<bool> CanAccessCreatedByRecordAsync(
            TaskDbcontext db,
            IRbacService rbac,
            int userId,
            string module,
            int? createdBy)
        {
            if (await rbac.IsAdminUserAsync(userId))
            {
                return true;
            }

            var scope = await rbac.GetModuleAccessScopeAsync(userId, module);
            if (scope is null or AccessScope.All)
            {
                return true;
            }

            if (createdBy == userId)
            {
                return true;
            }

            if (scope == AccessScope.Own || !createdBy.HasValue)
            {
                return false;
            }

            if (scope == AccessScope.Team)
            {
                var roleId = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.RoleId)
                    .FirstOrDefaultAsync();

                if (roleId == null)
                {
                    return false;
                }

                return await db.Users.AsNoTracking()
                    .AnyAsync(u => u.Id == createdBy && u.IsActive && u.RoleId == roleId);
            }

            return false;
        }
    }
}
