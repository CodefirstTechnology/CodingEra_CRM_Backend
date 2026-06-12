using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDashboardPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_dashboard_preferences",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    morning_briefing_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_briefing_played_date = table.Column<DateOnly>(type: "date", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_dashboard_preferences", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_dashboard_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_dashboard_preferences");
        }
    }
}
