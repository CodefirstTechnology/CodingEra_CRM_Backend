using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    [Migration("20260516130000_UserRoleForeignKey")]
    public class UserRoleForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role_id",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE users u
SET role_id = r.id
FROM crm_roles r
WHERE LOWER(TRIM(r.name)) = LOWER(TRIM(u.role)) AND r.is_active = true;
");

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_crm_roles_role_id",
                table: "users",
                column: "role_id",
                principalTable: "crm_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "user");

            migrationBuilder.Sql(@"
UPDATE users u
SET role = r.name
FROM crm_roles r
WHERE u.role_id IS NOT NULL AND r.id = u.role_id;
");

            migrationBuilder.DropForeignKey(
                name: "FK_users_crm_roles_role_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_role_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "users");
        }
    }
}
