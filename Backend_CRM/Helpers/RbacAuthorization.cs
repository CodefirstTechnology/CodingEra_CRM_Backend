using CRM.DATA;

using CRM.Services;

using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Logging;



namespace CRM.Helpers

{

    public static class RbacAuthorization

    {

        public static async Task<IActionResult?> RequirePermissionAsync(

            TaskDbcontext db,

            IRbacService rbac,

            int userId,

            string permissionCode,

            ILogger? logger = null)

        {

            return await RequireAnyPermissionAsync(db, rbac, userId, logger, permissionCode);

        }



        public static async Task<IActionResult?> RequireAnyPermissionAsync(

            TaskDbcontext db,

            IRbacService rbac,

            int userId,

            ILogger? logger,

            params string[] permissionCodes)

        {

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(db, userId);

            if (auditErr != null)

            {

                return auditErr;

            }



            foreach (var code in permissionCodes)

            {

                if (await rbac.HasPermissionAsync(userId, code))

                {

                    return null;

                }

            }



            var diagnostic = await rbac.GetAccessDiagnosticAsync(userId);

            logger?.LogWarning(

                "RBAC denied UserId={UserId} RoleId={RoleId} Role={RoleName} IsAdmin={IsAdmin} Required=[{Required}] Actual=[{Actual}]",

                diagnostic.UserId,

                diagnostic.RoleId,

                diagnostic.RoleName,

                diagnostic.IsAdminUser,

                string.Join(", ", permissionCodes),

                string.Join(", ", diagnostic.PermissionCodes));



            return ApiForbiddenResult.Create(

                "You do not have permission to perform this action.",

                new

                {

                    userId = diagnostic.UserId,

                    roleId = diagnostic.RoleId,

                    roleName = diagnostic.RoleName,

                    isAdminUser = diagnostic.IsAdminUser,

                    requiredPermissions = permissionCodes,

                    actualPermissions = diagnostic.PermissionCodes,

                });

        }



        public static Task<IActionResult?> RequireAnyPermissionAsync(

            TaskDbcontext db,

            IRbacService rbac,

            int userId,

            params string[] permissionCodes)

        {

            return RequireAnyPermissionAsync(db, rbac, userId, null, permissionCodes);

        }

    }

}


