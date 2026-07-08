using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRbacPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS crm_permissions (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    module character varying(64) NOT NULL,
    action character varying(64) NOT NULL,
    description text NOT NULL DEFAULT '',
    code character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_crm_permissions_code"" ON crm_permissions (code);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS crm_role_permissions (
    role_id integer NOT NULL,
    permission_id integer NOT NULL,
    access_scope integer NOT NULL DEFAULT 0,
    PRIMARY KEY (role_id, permission_id),
    CONSTRAINT ""FK_crm_role_permissions_role_id"" FOREIGN KEY (role_id) REFERENCES crm_roles (id) ON DELETE CASCADE,
    CONSTRAINT ""FK_crm_role_permissions_permission_id"" FOREIGN KEY (permission_id) REFERENCES crm_permissions (id) ON DELETE CASCADE
);
");

            // Seed permissions (idempotent)
            migrationBuilder.Sql(@"
INSERT INTO crm_permissions (module, action, description, code, created_at)
SELECT v.module, v.action, v.description, v.code, NOW() AT TIME ZONE 'utc'
FROM (VALUES
  ('leads', 'view', 'View leads', 'leads.view'),
  ('leads', 'create', 'Create leads', 'leads.create'),
  ('leads', 'edit', 'Edit leads', 'leads.edit'),
  ('leads', 'delete', 'Delete leads', 'leads.delete'),
  ('leads', 'assign', 'Assign leads', 'leads.assign'),
  ('leads', 'import', 'Import leads', 'leads.import'),
  ('leads', 'export', 'Export leads', 'leads.export'),
  ('deals', 'view', 'View deals', 'deals.view'),
  ('deals', 'create', 'Create deals', 'deals.create'),
  ('deals', 'edit', 'Edit deals', 'deals.edit'),
  ('deals', 'delete', 'Delete deals', 'deals.delete'),
  ('deals', 'assign', 'Assign deals', 'deals.assign'),
  ('deals', 'change_status', 'Change deal status', 'deals.change_status'),
  ('contacts', 'view', 'View contacts', 'contacts.view'),
  ('contacts', 'create', 'Create contacts', 'contacts.create'),
  ('contacts', 'edit', 'Edit contacts', 'contacts.edit'),
  ('contacts', 'delete', 'Delete contacts', 'contacts.delete'),
  ('organizations', 'view', 'View organizations', 'organizations.view'),
  ('organizations', 'create', 'Create organizations', 'organizations.create'),
  ('organizations', 'edit', 'Edit organizations', 'organizations.edit'),
  ('organizations', 'delete', 'Delete organizations', 'organizations.delete'),
  ('quotations', 'view', 'View quotations', 'quotations.view'),
  ('quotations', 'create', 'Create quotations', 'quotations.create'),
  ('quotations', 'edit', 'Edit quotations', 'quotations.edit'),
  ('quotations', 'delete', 'Delete quotations', 'quotations.delete'),
  ('quotations', 'approve', 'Approve quotations', 'quotations.approve'),
  ('tasks', 'view', 'View tasks', 'tasks.view'),
  ('tasks', 'create', 'Create tasks', 'tasks.create'),
  ('tasks', 'edit', 'Edit tasks', 'tasks.edit'),
  ('tasks', 'delete', 'Delete tasks', 'tasks.delete'),
  ('notes', 'view', 'View notes', 'notes.view'),
  ('notes', 'create', 'Create notes', 'notes.create'),
  ('notes', 'edit', 'Edit notes', 'notes.edit'),
  ('notes', 'delete', 'Delete notes', 'notes.delete'),
  ('users', 'view', 'View users', 'users.view'),
  ('users', 'create', 'Create users', 'users.create'),
  ('users', 'edit', 'Edit users', 'users.edit'),
  ('users', 'delete', 'Delete users', 'users.delete'),
  ('settings', 'view', 'View settings', 'settings.view'),
  ('settings', 'manage', 'Manage settings', 'settings.manage'),
  ('email', 'view', 'View emails', 'email.view'),
  ('email', 'send', 'Send emails', 'email.send'),
  ('email', 'configure', 'Configure email', 'email.configure'),
  ('roles', 'view', 'View roles', 'roles.view'),
  ('roles', 'manage', 'Manage roles and permissions', 'roles.manage')
) AS v(module, action, description, code)
WHERE NOT EXISTS (SELECT 1 FROM crm_permissions p WHERE p.code = v.code);
");

            // Seed additional default roles (keep existing user/admin)
            migrationBuilder.Sql(@"
INSERT INTO crm_roles (name, description, is_active, last_modified)
SELECT v.name, v.description, true, NOW() AT TIME ZONE 'utc'
FROM (VALUES
  ('Admin', 'Full system access'),
  ('Corporate Sales', 'Team-level sales access'),
  ('Sales', 'Own-record sales access'),
  ('Accounts', 'Accounts and quotations'),
  ('HR', 'Human resources access'),
  ('Marketing', 'Marketing access'),
  ('Operations', 'Operations access')
) AS v(name, description)
WHERE NOT EXISTS (SELECT 1 FROM crm_roles r WHERE LOWER(r.name) = LOWER(v.name));
");

            // Rename legacy admin role display name if needed (keep id)
            migrationBuilder.Sql(@"
UPDATE crm_roles SET name = 'Admin', description = 'Full system access'
WHERE LOWER(name) = 'admin' AND name <> 'Admin';
");

            // Admin role: all permissions, All scope (2)
            migrationBuilder.Sql(@"
INSERT INTO crm_role_permissions (role_id, permission_id, access_scope)
SELECT r.id, p.id, 2
FROM crm_roles r
CROSS JOIN crm_permissions p
WHERE LOWER(r.name) IN ('admin')
  AND NOT EXISTS (
    SELECT 1 FROM crm_role_permissions rp
    WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
");

            // Legacy user role: own-scope CRM permissions
            migrationBuilder.Sql(@"
INSERT INTO crm_role_permissions (role_id, permission_id, access_scope)
SELECT r.id, p.id, 0
FROM crm_roles r
JOIN crm_permissions p ON p.code IN (
  'leads.view','leads.create','leads.edit',
  'deals.view','deals.create','deals.edit',
  'contacts.view','contacts.create','contacts.edit',
  'organizations.view','organizations.create','organizations.edit',
  'quotations.view','quotations.create','quotations.edit',
  'tasks.view','tasks.create','tasks.edit',
  'notes.view','notes.create','notes.edit',
  'settings.view'
)
WHERE LOWER(r.name) = 'user'
  AND NOT EXISTS (
    SELECT 1 FROM crm_role_permissions rp
    WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
");

            // Sales role
            migrationBuilder.Sql(@"
INSERT INTO crm_role_permissions (role_id, permission_id, access_scope)
SELECT r.id, p.id,
  CASE WHEN p.code IN ('leads.view','leads.create','leads.edit','leads.assign',
    'deals.view','deals.create','deals.edit','deals.assign','deals.change_status',
    'quotations.view','quotations.create','quotations.edit',
    'contacts.view','contacts.create','contacts.edit',
    'tasks.view','tasks.create','tasks.edit',
    'notes.view','notes.create','notes.edit') THEN 0 ELSE 0 END
FROM crm_roles r
JOIN crm_permissions p ON p.code IN (
  'leads.view','leads.create','leads.edit','leads.assign',
  'deals.view','deals.create','deals.edit','deals.assign','deals.change_status',
  'quotations.view','quotations.create','quotations.edit',
  'contacts.view','contacts.create','contacts.edit',
  'tasks.view','tasks.create','tasks.edit',
  'notes.view','notes.create','notes.edit',
  'settings.view'
)
WHERE LOWER(r.name) = 'sales'
  AND NOT EXISTS (
    SELECT 1 FROM crm_role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
");

            // Corporate Sales: team scope (1) for leads/deals/quotations
            migrationBuilder.Sql(@"
INSERT INTO crm_role_permissions (role_id, permission_id, access_scope)
SELECT r.id, p.id,
  CASE
    WHEN p.module IN ('leads','deals','quotations') THEN 1
    ELSE 0
  END
FROM crm_roles r
JOIN crm_permissions p ON (
  (p.module = 'leads' AND p.action IN ('view','create','edit','assign','export'))
  OR (p.module = 'deals' AND p.action IN ('view','create','edit','assign','change_status'))
  OR (p.module = 'quotations' AND p.action IN ('view','create','edit'))
  OR (p.module IN ('contacts','tasks','notes') AND p.action IN ('view','create','edit'))
  OR p.code = 'settings.view'
)
WHERE LOWER(r.name) = 'corporate sales'
  AND NOT EXISTS (
    SELECT 1 FROM crm_role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
");

            // Accounts
            migrationBuilder.Sql(@"
INSERT INTO crm_role_permissions (role_id, permission_id, access_scope)
SELECT r.id, p.id, 2
FROM crm_roles r
JOIN crm_permissions p ON p.code IN (
  'quotations.view','quotations.approve','quotations.edit',
  'contacts.view','organizations.view','deals.view','settings.view'
)
WHERE LOWER(r.name) = 'accounts'
  AND NOT EXISTS (
    SELECT 1 FROM crm_role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
");

            // HR, Marketing, Operations: basic view + settings.view
            migrationBuilder.Sql(@"
INSERT INTO crm_role_permissions (role_id, permission_id, access_scope)
SELECT r.id, p.id, 0
FROM crm_roles r
JOIN crm_permissions p ON p.code IN ('leads.view','contacts.view','settings.view')
WHERE LOWER(r.name) IN ('hr','marketing','operations')
  AND NOT EXISTS (
    SELECT 1 FROM crm_role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS crm_role_permissions;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS crm_permissions;");
        }
    }
}
