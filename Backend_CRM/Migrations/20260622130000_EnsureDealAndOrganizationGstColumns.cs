using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Model snapshot included <c>gst</c> on deals/organizations before any migration created the columns (DB drift).
    /// Idempotent SQL so auto-migrate is safe on production and when columns were added manually.
    /// </summary>
    public partial class EnsureDealAndOrganizationGstColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE deals ADD COLUMN IF NOT EXISTS gst character varying(32) NOT NULL DEFAULT '';
                ALTER TABLE organizations ADD COLUMN IF NOT EXISTS gst character varying(32) NOT NULL DEFAULT '';
                ALTER TABLE quotations ADD COLUMN IF NOT EXISTS gst character varying(32) NOT NULL DEFAULT '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE deals DROP COLUMN IF EXISTS gst;
                ALTER TABLE organizations DROP COLUMN IF EXISTS gst;
                """);
        }
    }
}
