using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class EnsureLeadAndDealFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE leads ADD COLUMN IF NOT EXISTS address text NOT NULL DEFAULT '';
                ALTER TABLE leads ADD COLUMN IF NOT EXISTS lead_date date NULL;
                ALTER TABLE lead_histories ADD COLUMN IF NOT EXISTS address text NOT NULL DEFAULT '';
                ALTER TABLE lead_histories ADD COLUMN IF NOT EXISTS lead_date date NULL;
                ALTER TABLE deals ADD COLUMN IF NOT EXISTS deal_amount numeric NULL;
                UPDATE leads SET lead_date = (created_at AT TIME ZONE 'UTC')::date WHERE lead_date IS NULL AND created_at IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
