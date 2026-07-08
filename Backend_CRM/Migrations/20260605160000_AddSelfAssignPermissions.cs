using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfAssignPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO crm_permissions (module, action, description, code, created_at)
SELECT v.module, v.action, v.description, v.code, NOW() AT TIME ZONE 'utc'
FROM (VALUES
  ('leads', 'self_assign', 'Self-assign lead owner on manual create', 'leads.self_assign'),
  ('deals', 'self_assign', 'Self-assign deal owner on manual create', 'deals.self_assign')
) AS v(module, action, description, code)
WHERE NOT EXISTS (SELECT 1 FROM crm_permissions p WHERE p.code = v.code);
");

            // Legacy User role: self-assign on manual create (no full assign)
            migrationBuilder.Sql(@"
INSERT INTO crm_role_permissions (role_id, permission_id, access_scope)
SELECT r.id, p.id, 0
FROM crm_roles r
JOIN crm_permissions p ON p.code IN ('leads.self_assign', 'deals.self_assign')
WHERE LOWER(r.name) = 'user'
  AND NOT EXISTS (
    SELECT 1 FROM crm_role_permissions rp
    WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
");

            // Sales: self-assign in addition to full assign (harmless fallback when assign is revoked)
            migrationBuilder.Sql(@"
INSERT INTO crm_role_permissions (role_id, permission_id, access_scope)
SELECT r.id, p.id, 0
FROM crm_roles r
JOIN crm_permissions p ON p.code IN ('leads.self_assign', 'deals.self_assign')
WHERE LOWER(r.name) = 'sales'
  AND NOT EXISTS (
    SELECT 1 FROM crm_role_permissions rp
    WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM crm_role_permissions
WHERE permission_id IN (
  SELECT id FROM crm_permissions WHERE code IN ('leads.self_assign', 'deals.self_assign')
);
DELETE FROM crm_permissions WHERE code IN ('leads.self_assign', 'deals.self_assign');
");
        }
    }
}
