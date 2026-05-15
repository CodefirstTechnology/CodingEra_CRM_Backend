using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class LeadNormalizeOrganizationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve denormalized lead company fields into organizations before dropping columns.
            migrationBuilder.Sql(
                """
                UPDATE organizations AS o
                SET
                    name = CASE WHEN COALESCE(TRIM(l.organization), '') <> '' THEN LEFT(TRIM(l.organization), 512) ELSE o.name END,
                    website = CASE WHEN COALESCE(TRIM(l.website), '') <> '' THEN LEFT(TRIM(l.website), 512) ELSE o.website END,
                    industry = CASE WHEN COALESCE(TRIM(l.industry), '') <> '' THEN LEFT(TRIM(l.industry), 256) ELSE o.industry END,
                    annual_revenue = COALESCE(l.annual_revenue, o.annual_revenue),
                    employees = CASE WHEN COALESCE(TRIM(l.employees), '') <> '' THEN LEFT(TRIM(l.employees), 128) ELSE o.employees END,
                    territory = CASE WHEN COALESCE(TRIM(l.territory), '') <> '' THEN LEFT(TRIM(l.territory), 256) ELSE o.territory END,
                    last_modified = NOW()
                FROM leads AS l
                WHERE l.organization_id = o.id;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    r RECORD;
                    new_id INTEGER;
                BEGIN
                    FOR r IN
                        SELECT id, organization, website, industry, annual_revenue, employees, territory
                        FROM leads
                        WHERE organization_id IS NULL AND LENGTH(TRIM(COALESCE(organization, ''))) > 0
                    LOOP
                        INSERT INTO organizations (name, website, industry, annual_revenue, employees, territory, address, last_modified)
                        VALUES (
                            LEFT(TRIM(r.organization), 512),
                            COALESCE(LEFT(TRIM(COALESCE(r.website, '')), 512), ''),
                            COALESCE(LEFT(TRIM(COALESCE(r.industry, '')), 256), ''),
                            r.annual_revenue,
                            COALESCE(LEFT(TRIM(COALESCE(r.employees, '')), 128), ''),
                            COALESCE(LEFT(TRIM(COALESCE(r.territory, '')), 256), ''),
                            '',
                            NOW()
                        )
                        RETURNING id INTO new_id;
                        UPDATE leads SET organization_id = new_id WHERE leads.id = r.id;
                    END LOOP;
                END;
                $$;
                """);

            migrationBuilder.DropColumn(
                name: "annual_revenue",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "employees",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "industry",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "organization",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "territory",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "website",
                table: "leads");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "annual_revenue",
                table: "leads",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employees",
                table: "leads",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "industry",
                table: "leads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "organization",
                table: "leads",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "territory",
                table: "leads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "website",
                table: "leads",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }
    }
}
