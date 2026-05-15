using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class SalutationIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "salutations",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "salutations");
        }
    }
}
