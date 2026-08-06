using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approval_remarks",
                table: "leave_requests",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "approved_by_user_id",
                table: "leave_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attachment_file_name",
                table: "leave_requests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attachment_path",
                table: "leave_requests",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_approved_by_user_id",
                table: "leave_requests",
                column: "approved_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_leave_requests_users_approved_by_user_id",
                table: "leave_requests",
                column: "approved_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateTable(
                name: "leave_notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    leave_request_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_notifications_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_notifications_leave_requests_leave_request_id",
                        column: x => x.leave_request_id,
                        principalTable: "leave_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leave_notifications_employee_id",
                table: "leave_notifications",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_notifications_is_read",
                table: "leave_notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "IX_leave_notifications_leave_request_id",
                table: "leave_notifications",
                column: "leave_request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "leave_notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_leave_requests_users_approved_by_user_id",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "IX_leave_requests_approved_by_user_id",
                table: "leave_requests");

            migrationBuilder.DropColumn(name: "approval_remarks", table: "leave_requests");
            migrationBuilder.DropColumn(name: "approved_at", table: "leave_requests");
            migrationBuilder.DropColumn(name: "approved_by_user_id", table: "leave_requests");
            migrationBuilder.DropColumn(name: "attachment_file_name", table: "leave_requests");
            migrationBuilder.DropColumn(name: "attachment_path", table: "leave_requests");
            migrationBuilder.DropColumn(name: "updated_at", table: "leave_requests");
        }
    }
}
