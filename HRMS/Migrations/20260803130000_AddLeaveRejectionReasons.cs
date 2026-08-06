using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRejectionReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leave_rejection_reasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_rejection_reasons", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leave_rejection_reasons_title",
                table: "leave_rejection_reasons",
                column: "title",
                unique: true);

            migrationBuilder.InsertData(
                table: "leave_rejection_reasons",
                columns: new[] { "id", "title", "is_active", "sort_order" },
                values: new object[,]
                {
                    { 1, "Project Delivery Deadline / Critical Milestone", true, 1 },
                    { 2, "Insufficient Leave Balance", true, 2 },
                    { 3, "Team Resource Shortage on Requested Dates", true, 3 },
                    { 4, "Overlapping Team Member Leave", true, 4 },
                    { 5, "Incomplete Information / Missing Attachments", true, 5 },
                    { 6, "Other Reason", true, 6 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "leave_rejection_reasons");
        }
    }
}
