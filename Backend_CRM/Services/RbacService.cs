using CRM.DATA;

using CRM.DTO;

using CRM.Helpers;

using CRM.models;

using Microsoft.EntityFrameworkCore;



namespace CRM.Services

{

    public class RbacService : IRbacService

    {

        private readonly TaskDbcontext _db;



        public RbacService(TaskDbcontext db)

        {

            _db = db;

        }



        public async Task<RbacAccessDiagnosticDto> GetAccessDiagnosticAsync(int userId)

        {

            var row = await _db.Users.AsNoTracking()

                .Where(u => u.Id == userId)

                .Select(u => new { u.RoleId, RoleName = u.Role != null ? u.Role.Name : string.Empty })

                .FirstOrDefaultAsync();



            var perms = await GetUserPermissionsAsync(userId);

            var isAdmin = await IsAdminUserAsync(userId);



            return new RbacAccessDiagnosticDto

            {

                UserId = userId,

                RoleId = row?.RoleId,

                RoleName = row?.RoleName ?? string.Empty,

                IsAdminUser = isAdmin,

                PermissionCodes = perms.Select(p => p.Code).OrderBy(c => c).ToList(),

            };

        }



        public async Task<bool> IsAdminUserAsync(int userId)

        {

            var row = await _db.Users.AsNoTracking()

                .Where(u => u.Id == userId && u.IsActive)

                .Select(u => new { u.RoleId, RoleName = u.Role != null ? u.Role.Name : string.Empty })

                .FirstOrDefaultAsync();



            if (row == null)

            {

                return false;

            }



            if (RbacAdminHelper.IsAdminRole(row.RoleId, row.RoleName))

            {

                return true;

            }



            if (row.RoleId == null)

            {

                return false;

            }



            return await _db.RolePermissions.AsNoTracking()

                .Where(rp => rp.RoleId == row.RoleId)

                .Join(

                    _db.Permissions.AsNoTracking(),

                    rp => rp.PermissionId,

                    p => p.Id,

                    (_, p) => p.Code)

                .AnyAsync(c => c == "settings.manage" || c == "roles.manage");

        }



        public async Task<IReadOnlyList<UserPermissionDto>> GetUserPermissionsAsync(int userId)

        {

            if (await IsAdminUserAsync(userId))

            {

                return await GetAllPermissionsAsync();

            }



            var roleId = await _db.Users.AsNoTracking()

                .Where(u => u.Id == userId && u.IsActive)

                .Select(u => u.RoleId)

                .FirstOrDefaultAsync();



            if (roleId == null)

            {

                return Array.Empty<UserPermissionDto>();

            }



            var perms = await _db.RolePermissions.AsNoTracking()

                .Where(rp => rp.RoleId == roleId)

                .Join(

                    _db.Permissions.AsNoTracking(),

                    rp => rp.PermissionId,

                    p => p.Id,

                    (rp, p) => new UserPermissionDto

                    {

                        Code = p.Code,

                        Module = p.Module,

                        Action = p.Action,

                        AccessScope = rp.AccessScope,

                    })

                .ToListAsync();



            if (perms.Count == 0)

            {

                return await GetLegacyFallbackPermissionsAsync(roleId.Value);

            }



            return perms;

        }



        public async Task<bool> HasPermissionAsync(int userId, string permissionCode)

        {

            if (string.IsNullOrWhiteSpace(permissionCode))

            {

                return false;

            }



            if (await IsAdminUserAsync(userId))

            {

                return true;

            }



            var code = permissionCode.Trim().ToLowerInvariant();

            var perms = await GetUserPermissionsAsync(userId);

            return perms.Any(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));

        }



        public async Task<AccessScope?> GetModuleAccessScopeAsync(int userId, string module)

        {

            if (string.IsNullOrWhiteSpace(module))

            {

                return null;

            }



            if (await IsAdminUserAsync(userId))

            {

                return AccessScope.All;

            }



            var mod = module.Trim().ToLowerInvariant();

            var perms = await GetUserPermissionsAsync(userId);

            var viewPerm = perms

                .Where(p => string.Equals(p.Module, mod, StringComparison.OrdinalIgnoreCase)

                    && string.Equals(p.Action, "view", StringComparison.OrdinalIgnoreCase))

                .Select(p => (AccessScope?)p.AccessScope)

                .DefaultIfEmpty()

                .Max();



            return viewPerm;

        }



        public async Task<bool> CanViewAllRecordsAsync(int userId)

        {

            if (await IsAdminUserAsync(userId))

            {

                return true;

            }



            if (await HasPermissionAsync(userId, "settings.manage"))

            {

                return true;

            }



            var perms = await GetUserPermissionsAsync(userId);

            return perms.Any(p => p.AccessScope == AccessScope.All);

        }



        public async Task<bool> CanManageRbacAsync(int userId)

        {

            return await HasPermissionAsync(userId, "roles.manage")

                || await HasPermissionAsync(userId, "settings.manage");

        }



        private async Task<IReadOnlyList<UserPermissionDto>> GetAllPermissionsAsync()

        {

            return await _db.Permissions.AsNoTracking()

                .Select(p => new UserPermissionDto

                {

                    Code = p.Code,

                    Module = p.Module,

                    Action = p.Action,

                    AccessScope = AccessScope.All,

                })

                .ToListAsync();

        }



        /// <summary>Backward compatibility when role has no rows in crm_role_permissions.</summary>

        private async Task<IReadOnlyList<UserPermissionDto>> GetLegacyFallbackPermissionsAsync(int roleId)

        {

            var roleName = await _db.Roles.AsNoTracking()

                .Where(r => r.Id == roleId)

                .Select(r => r.Name)

                .FirstOrDefaultAsync();



            if (RbacAdminHelper.IsAdminRole(roleId, roleName))

            {

                return await GetAllPermissionsAsync();

            }



            if (CrmRoleHelper.IsSuperAdminRoleName(roleName))

            {

                return await GetAllPermissionsAsync();

            }



            return GetDefaultUserPermissions();

        }



        private static IReadOnlyList<UserPermissionDto> GetDefaultUserPermissions()

        {

            var modules = new[] { "leads", "deals", "contacts", "organizations", "quotations", "tasks", "notes" };

            var actions = new[] { "view", "create", "edit" };

            var list = new List<UserPermissionDto>();

            foreach (var mod in modules)

            {

                foreach (var action in actions)

                {

                    list.Add(new UserPermissionDto

                    {

                        Module = mod,

                        Action = action,

                        Code = $"{mod}.{action}",

                        AccessScope = AccessScope.Own,

                    });

                }

            }



            list.Add(new UserPermissionDto

            {

                Module = "settings",

                Action = "view",

                Code = "settings.view",

                AccessScope = AccessScope.Own,

            });



            list.Add(new UserPermissionDto { Module = "leads", Action = "self_assign", Code = "leads.self_assign", AccessScope = AccessScope.Own });

            list.Add(new UserPermissionDto { Module = "deals", Action = "self_assign", Code = "deals.self_assign", AccessScope = AccessScope.Own });



            return list;

        }

    }

}


