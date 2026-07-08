using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class QuotationItemMasterIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS gst_percent numeric NOT NULL DEFAULT 0.0;
                ALTER TABLE quotation_line_items ADD COLUMN IF NOT EXISTS item_id integer;
                ALTER TABLE quotation_line_items ADD COLUMN IF NOT EXISTS item_snapshot_json text NOT NULL DEFAULT '';
                ALTER TABLE quotation_line_items ADD COLUMN IF NOT EXISTS steel_rate numeric NOT NULL DEFAULT 0.0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gst_percent",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "item_id",
                table: "quotation_line_items");

            migrationBuilder.DropColumn(
                name: "item_snapshot_json",
                table: "quotation_line_items");

            migrationBuilder.DropColumn(
                name: "steel_rate",
                table: "quotation_line_items");
        }
    }
}
