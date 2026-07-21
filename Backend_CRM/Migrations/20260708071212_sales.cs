using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class sales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columns may already exist from AddQuotationTemplate or QuotationTemplateSchemaEnsure on startup.
            migrationBuilder.Sql(
                """
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS quotation_template character varying(32) NOT NULL DEFAULT 'Standard';
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS template_payload_json text NOT NULL DEFAULT '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE quotations DROP COLUMN IF EXISTS quotation_template;
                ALTER TABLE quotations DROP COLUMN IF EXISTS template_payload_json;
                """);
        }
    }
}
