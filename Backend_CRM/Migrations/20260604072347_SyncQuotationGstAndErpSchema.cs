using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class SyncQuotationGstAndErpSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "subtotal",
                table: "quotations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_total",
                table: "quotations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_quantity",
                table: "quotations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_weight",
                table: "quotations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_percent",
                table: "quotation_line_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "gst_percent",
                table: "quotation_line_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "item_name",
                table: "quotation_line_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "line_total",
                table: "quotation_line_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "quotation_line_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_weight",
                table: "quotation_line_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "weight",
                table: "quotation_line_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "quotation_item_grid_defaults",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    columns_json = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_item_grid_defaults", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quotation_item_grid_user_preferences",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    columns_json = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_item_grid_user_preferences", x => x.user_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quotation_item_grid_defaults");

            migrationBuilder.DropTable(
                name: "quotation_item_grid_user_preferences");

            migrationBuilder.DropColumn(
                name: "subtotal",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "tax_total",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "total_quantity",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "total_weight",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "discount_percent",
                table: "quotation_line_items");

            migrationBuilder.DropColumn(
                name: "gst_percent",
                table: "quotation_line_items");

            migrationBuilder.DropColumn(
                name: "item_name",
                table: "quotation_line_items");

            migrationBuilder.DropColumn(
                name: "line_total",
                table: "quotation_line_items");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "quotation_line_items");

            migrationBuilder.DropColumn(
                name: "unit_weight",
                table: "quotation_line_items");

            migrationBuilder.DropColumn(
                name: "weight",
                table: "quotation_line_items");
        }
    }
}
