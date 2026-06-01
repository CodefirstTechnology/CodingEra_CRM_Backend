using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class ir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quotation_fiscal_sequences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fiscal_year_label = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    last_sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_fiscal_sequences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quotation_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    document_type_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    deal_id = table.Column<int>(type: "integer", nullable: true),
                    salutation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    company_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    employees = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    annual_revenue = table.Column<decimal>(type: "numeric", nullable: true),
                    website = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    territory = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    industry = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email_address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    office_address = table.Column<string>(type: "text", nullable: false),
                    site_address = table.Column<string>(type: "text", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reference_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    document_type_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fiscal_year_label = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    quotation_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    quotation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: false),
                    grand_total = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quotation_line_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quotation_id = table.Column<int>(type: "integer", nullable: false),
                    line_index = table.Column<int>(type: "integer", nullable: false),
                    item_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    uom = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_line_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_quotation_line_items_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quotation_fiscal_sequences_company_code_fiscal_year_label",
                table: "quotation_fiscal_sequences",
                columns: new[] { "company_code", "fiscal_year_label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotation_line_items_quotation_id",
                table: "quotation_line_items",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_company_code_fiscal_year_label_sequence_number",
                table: "quotations",
                columns: new[] { "company_code", "fiscal_year_label", "sequence_number" });

            migrationBuilder.CreateIndex(
                name: "IX_quotations_quotation_number",
                table: "quotations",
                column: "quotation_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quotation_fiscal_sequences");

            migrationBuilder.DropTable(
                name: "quotation_line_items");

            migrationBuilder.DropTable(
                name: "quotation_settings");

            migrationBuilder.DropTable(
                name: "quotations");
        }
    }
}
