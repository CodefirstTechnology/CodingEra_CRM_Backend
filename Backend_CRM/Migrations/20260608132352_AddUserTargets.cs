using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_target_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_target_types", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_target_types_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_target_types_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_targets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    target_type_id = table.Column<int>(type: "integer", nullable: false),
                    target_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    achieved_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    achieved_calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_targets", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_targets_user_target_types_target_type_id",
                        column: x => x.target_type_id,
                        principalTable: "user_target_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_targets_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_targets_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_targets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_target_types_created_by",
                table: "user_target_types",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_target_types_name",
                table: "user_target_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_target_types_updated_by",
                table: "user_target_types",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_targets_created_by",
                table: "user_targets",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_targets_is_active",
                table: "user_targets",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_user_targets_target_type_id",
                table: "user_targets",
                column: "target_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_targets_updated_by",
                table: "user_targets",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_targets_user_id_target_type_id_start_date_end_date",
                table: "user_targets",
                columns: new[] { "user_id", "target_type_id", "start_date", "end_date" });

            migrationBuilder.Sql(@"
INSERT INTO user_target_types (name, description, sort_order, is_active, created_at, updated_at, last_modified)
SELECT v.name, v.description, v.sort_order, true, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
FROM (VALUES
  ('Hourly Target', 'Sales target measured per hour', 1),
  ('Daily Target', 'Sales target measured per day', 2),
  ('Weekly Target', 'Sales target measured per week', 3),
  ('Monthly Target', 'Sales target measured per month', 4)
) AS v(name, description, sort_order)
WHERE NOT EXISTS (
  SELECT 1 FROM user_target_types t WHERE LOWER(t.name) = LOWER(v.name)
);
");

            migrationBuilder.Sql(@"
INSERT INTO crm_permissions (module, action, code, description, created_at)
SELECT v.module, v.action, v.code, v.description, NOW() AT TIME ZONE 'UTC'
FROM (VALUES
  ('user_targets', 'view', 'user_targets.view', 'View sales user targets'),
  ('user_targets', 'manage', 'user_targets.manage', 'Manage sales user targets')
) AS v(module, action, code, description)
WHERE NOT EXISTS (SELECT 1 FROM crm_permissions p WHERE p.code = v.code);
");

            migrationBuilder.Sql(@"
INSERT INTO crm_role_permissions (role_id, permission_id, access_scope)
SELECT r.id, p.id, 0
FROM crm_roles r
JOIN crm_permissions p ON p.code = 'user_targets.view'
WHERE LOWER(r.name) = 'sales'
  AND NOT EXISTS (
    SELECT 1 FROM crm_role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM crm_role_permissions rp
USING crm_permissions p
WHERE rp.permission_id = p.id AND p.code IN ('user_targets.view', 'user_targets.manage');
");

            migrationBuilder.Sql(@"
DELETE FROM crm_permissions WHERE code IN ('user_targets.view', 'user_targets.manage');
");

            migrationBuilder.DropTable(
                name: "user_targets");

            migrationBuilder.DropTable(
                name: "user_target_types");
        }
    }
}
