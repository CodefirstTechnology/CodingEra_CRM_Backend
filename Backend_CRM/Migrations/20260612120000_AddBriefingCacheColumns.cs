using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddBriefingCacheColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "cached_briefing_date",
                table: "user_dashboard_preferences",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cached_briefing_message",
                table: "user_dashboard_preferences",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cached_briefing_source",
                table: "user_dashboard_preferences",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cached_briefing_date",
                table: "user_dashboard_preferences");

            migrationBuilder.DropColumn(
                name: "cached_briefing_message",
                table: "user_dashboard_preferences");

            migrationBuilder.DropColumn(
                name: "cached_briefing_source",
                table: "user_dashboard_preferences");
        }
    }
}
