using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationTermsConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS customize_terms boolean NOT NULL DEFAULT false;
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS intro_text text NOT NULL DEFAULT '';
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS transportation_label character varying(128) NOT NULL DEFAULT '';
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS jurisdiction character varying(256) NOT NULL DEFAULT '';
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS terms_conditions_json text NOT NULL DEFAULT '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE quotations DROP COLUMN IF EXISTS customize_terms;
                ALTER TABLE quotations DROP COLUMN IF EXISTS intro_text;
                ALTER TABLE quotations DROP COLUMN IF EXISTS transportation_label;
                ALTER TABLE quotations DROP COLUMN IF EXISTS jurisdiction;
                ALTER TABLE quotations DROP COLUMN IF EXISTS terms_conditions_json;
                """);
        }
    }
}
