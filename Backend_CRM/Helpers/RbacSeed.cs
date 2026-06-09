using CRM.DATA;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent RBAC seed — ensures permissions exist and Admin role has full access.</summary>
    public static class RbacSeed
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsurePermissionsAsync(db, cancellationToken);
                await EnsureAdminRolePermissionsAsync(db, logger, cancellationToken);
                logger.LogInformation("RBAC seed verified (permissions + Admin role mappings).");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RBAC seed could not complete (tables may not exist yet).");
            }
        }

        private static async Task EnsurePermissionsAsync(TaskDbcontext db, CancellationToken cancellationToken)
        {
            var seeds = new (string Module, string Action, string Code, string Description)[]
            {
                ("leads", "view", "leads.view", "View leads"),
                ("leads", "create", "leads.create", "Create leads"),
                ("leads", "edit", "leads.edit", "Edit leads"),
                ("leads", "delete", "leads.delete", "Delete leads"),
                ("leads", "assign", "leads.assign", "Assign leads"),
                ("leads", "self_assign", "leads.self_assign", "Self-assign lead owner on manual create"),
                ("leads", "import", "leads.import", "Import leads"),
                ("leads", "export", "leads.export", "Export leads"),
                ("deals", "view", "deals.view", "View deals"),
                ("deals", "create", "deals.create", "Create deals"),
                ("deals", "edit", "deals.edit", "Edit deals"),
                ("deals", "delete", "deals.delete", "Delete deals"),
                ("deals", "assign", "deals.assign", "Assign deals"),
                ("deals", "self_assign", "deals.self_assign", "Self-assign deal owner on manual create"),
                ("deals", "change_status", "deals.change_status", "Change deal status"),
                ("contacts", "view", "contacts.view", "View contacts"),
                ("contacts", "create", "contacts.create", "Create contacts"),
                ("contacts", "edit", "contacts.edit", "Edit contacts"),
                ("contacts", "delete", "contacts.delete", "Delete contacts"),
                ("organizations", "view", "organizations.view", "View organizations"),
                ("organizations", "create", "organizations.create", "Create organizations"),
                ("organizations", "edit", "organizations.edit", "Edit organizations"),
                ("organizations", "delete", "organizations.delete", "Delete organizations"),
                ("quotations", "view", "quotations.view", "View quotations"),
                ("quotations", "create", "quotations.create", "Create quotations"),
                ("quotations", "edit", "quotations.edit", "Edit quotations"),
                ("quotations", "delete", "quotations.delete", "Delete quotations"),
                ("quotations", "approve", "quotations.approve", "Approve quotations"),
                ("tasks", "view", "tasks.view", "View tasks"),
                ("tasks", "create", "tasks.create", "Create tasks"),
                ("tasks", "edit", "tasks.edit", "Edit tasks"),
                ("tasks", "delete", "tasks.delete", "Delete tasks"),
                ("notes", "view", "notes.view", "View notes"),
                ("notes", "create", "notes.create", "Create notes"),
                ("notes", "edit", "notes.edit", "Edit notes"),
                ("notes", "delete", "notes.delete", "Delete notes"),
                ("users", "view", "users.view", "View users"),
                ("users", "create", "users.create", "Create users"),
                ("users", "edit", "users.edit", "Edit users"),
                ("users", "delete", "users.delete", "Delete users"),
                ("settings", "view", "settings.view", "View settings"),
                ("settings", "manage", "settings.manage", "Manage settings"),
                ("email", "view", "email.view", "View emails"),
                ("email", "send", "email.send", "Send emails"),
                ("email", "configure", "email.configure", "Configure email"),
                ("roles", "view", "roles.view", "View roles"),
                ("roles", "manage", "roles.manage", "Manage roles and permissions"),
                ("items", "view", "items.view", "View item master"),
                ("items", "manage", "items.manage", "Manage item master"),
                ("user_targets", "view", "user_targets.view", "View sales user targets"),
                ("user_targets", "manage", "user_targets.manage", "Manage sales user targets"),
            };

            foreach (var s in seeds)
            {
                if (await db.Permissions.AnyAsync(p => p.Code == s.Code, cancellationToken))
                {
                    continue;
                }

                await db.Permissions.AddAsync(new Permission
                {
                    Module = s.Module,
                    Action = s.Action,
                    Code = s.Code,
                    Description = s.Description,
                    CreatedAt = DateTime.UtcNow,
                }, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureAdminRolePermissionsAsync(
            TaskDbcontext db,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var adminRoles = await db.Roles.AsNoTracking()
                .Where(r => r.IsActive && (
                    r.Id == AdminUserValidation.AdminRoleId
                    || r.Name.ToLower() == "admin"
                    || r.Name.ToLower() == "administrator"))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (adminRoles.Count == 0)
            {
                logger.LogWarning("RBAC seed: no Admin role found in crm_roles.");
                return;
            }

            var allPermissionIds = await db.Permissions.AsNoTracking()
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            if (allPermissionIds.Count == 0)
            {
                return;
            }

            foreach (var roleId in adminRoles)
            {
                var existing = await db.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync(cancellationToken);

                var missing = allPermissionIds.Except(existing).ToList();
                if (missing.Count == 0)
                {
                    continue;
                }

                foreach (var permissionId in missing)
                {
                    await db.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId,
                        AccessScope = AccessScope.All,
                    }, cancellationToken);
                }

                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation(
                    "RBAC seed: added {Count} permission(s) to Admin role id {RoleId}.",
                    missing.Count,
                    roleId);
            }
        }
    }
}
