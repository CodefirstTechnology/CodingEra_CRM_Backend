using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "code", "description", "is_active", "name" },
                values: new object[,]
                {
                    { 1, "SUPER_ADMIN", "Full system access including HR admin and system settings.", true, "Super Admin" },
                    { 2, "HR_ADMIN", "HR operations: employees, payroll, leave approval, reports.", true, "HR Admin" },
                    { 3, "EMPLOYEE", "Self-service access to own profile, attendance, leave, and payslips.", true, "Employee" }
                });

            migrationBuilder.AddColumn<int>(
                name: "role_id",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE users u
                SET role_id = r.id
                FROM roles r
                WHERE u.role = r.code
                """);

            migrationBuilder.Sql("""
                UPDATE users
                SET role_id = 3
                WHERE role_id IS NULL
                """);

            migrationBuilder.AlterColumn<int>(
                name: "role_id",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_users_role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_role_id",
                table: "users",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                "SELECT setval(pg_get_serial_sequence('roles', 'id'), COALESCE((SELECT MAX(id) FROM roles), 0), true)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_role_id",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "EMPLOYEE");

            migrationBuilder.Sql("""
                UPDATE users u
                SET role = r.code
                FROM roles r
                WHERE u.role_id = r.id
                """);

            migrationBuilder.DropIndex(
                name: "IX_users_role_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.CreateIndex(
                name: "IX_users_role",
                table: "users",
                column: "role");
        }
    }
}
