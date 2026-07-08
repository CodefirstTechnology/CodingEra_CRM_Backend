using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationAdditionalCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS gst character varying(32) NOT NULL DEFAULT '';
                """);

            migrationBuilder.AddColumn<decimal>(
                name: "loading_charges",
                table: "quotations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "service_charges",
                table: "quotations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "transportation_charges",
                table: "quotations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "quotation_additional_charges",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quotation_id = table.Column<int>(type: "integer", nullable: false),
                    charge_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    sort_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_additional_charges", x => x.id);
                    table.ForeignKey(
                        name: "FK_quotation_additional_charges_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quotation_additional_charges_quotation_id",
                table: "quotation_additional_charges",
                column: "quotation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quotation_additional_charges");

            migrationBuilder.DropColumn(
                name: "loading_charges",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "service_charges",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "transportation_charges",
                table: "quotations");
        }
    }
}
