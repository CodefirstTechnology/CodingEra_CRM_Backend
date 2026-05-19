using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class LeadOrganizationMasterForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "employee_count_id",
                table: "organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "industry_id",
                table: "organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "territory_id",
                table: "organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lead_status_id",
                table: "leads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "request_type_id",
                table: "leads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "salutation_id",
                table: "leads",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE leads AS l SET salutation_id = s.id FROM salutations s
                WHERE l.salutation IS NOT NULL AND btrim(l.salutation) <> ''
                  AND lower(l.salutation) = lower(s.name);

                UPDATE leads AS l SET lead_status_id = ls.id FROM lead_statuses ls
                WHERE l.status IS NOT NULL AND btrim(l.status) <> ''
                  AND lower(l.status) = lower(ls.name);

                UPDATE leads AS l SET request_type_id = rt.id FROM request_types rt
                WHERE l.request_type IS NOT NULL AND btrim(l.request_type) <> ''
                  AND lower(l.request_type) = lower(rt.name);

                UPDATE organizations AS o SET industry_id = i.id FROM industries i
                WHERE o.industry IS NOT NULL AND btrim(o.industry) <> ''
                  AND lower(o.industry) = lower(i.name);

                UPDATE organizations AS o SET employee_count_id = e.id FROM employee_counts e
                WHERE o.employees IS NOT NULL AND btrim(o.employees) <> ''
                  AND lower(o.employees) = lower(e.name);

                UPDATE organizations AS o SET territory_id = t.id FROM territories t
                WHERE o.territory IS NOT NULL AND btrim(o.territory) <> ''
                  AND lower(o.territory) = lower(t.name);
                """);

            migrationBuilder.DropColumn(
                name: "employees",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "industry",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "territory",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "request_type",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "salutation",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "status",
                table: "leads");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_employee_count_id",
                table: "organizations",
                column: "employee_count_id");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_industry_id",
                table: "organizations",
                column: "industry_id");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_territory_id",
                table: "organizations",
                column: "territory_id");

            migrationBuilder.CreateIndex(
                name: "IX_leads_lead_status_id",
                table: "leads",
                column: "lead_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_leads_request_type_id",
                table: "leads",
                column: "request_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_leads_salutation_id",
                table: "leads",
                column: "salutation_id");

            migrationBuilder.AddForeignKey(
                name: "FK_leads_lead_statuses_lead_status_id",
                table: "leads",
                column: "lead_status_id",
                principalTable: "lead_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_request_types_request_type_id",
                table: "leads",
                column: "request_type_id",
                principalTable: "request_types",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_salutations_salutation_id",
                table: "leads",
                column: "salutation_id",
                principalTable: "salutations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_organizations_employee_counts_employee_count_id",
                table: "organizations",
                column: "employee_count_id",
                principalTable: "employee_counts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_organizations_industries_industry_id",
                table: "organizations",
                column: "industry_id",
                principalTable: "industries",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_organizations_territories_territory_id",
                table: "organizations",
                column: "territory_id",
                principalTable: "territories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leads_lead_statuses_lead_status_id",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_request_types_request_type_id",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_salutations_salutation_id",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_organizations_employee_counts_employee_count_id",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_organizations_industries_industry_id",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_organizations_territories_territory_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_organizations_employee_count_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_organizations_industry_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_organizations_territory_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_leads_lead_status_id",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_leads_request_type_id",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_leads_salutation_id",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "employee_count_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "industry_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "territory_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "lead_status_id",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "request_type_id",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "salutation_id",
                table: "leads");

            migrationBuilder.AddColumn<string>(
                name: "employees",
                table: "organizations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "industry",
                table: "organizations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "territory",
                table: "organizations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "request_type",
                table: "leads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "salutation",
                table: "leads",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "leads",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}
