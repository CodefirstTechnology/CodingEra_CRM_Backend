using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyLeadAndOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leads_external_ref",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "address",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "city",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "external_ref",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "job_title",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "message",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "name",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "product",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "sort_timestamp",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "source",
                table: "leads");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "organizations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "leads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_ref",
                table: "leads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "job_title",
                table: "leads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "message",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "leads",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "product",
                table: "leads",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "leads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "sort_timestamp",
                table: "leads",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "leads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_leads_external_ref",
                table: "leads",
                column: "external_ref",
                unique: true,
                filter: "\"external_ref\" IS NOT NULL AND \"external_ref\" <> ''");
        }
    }
}
