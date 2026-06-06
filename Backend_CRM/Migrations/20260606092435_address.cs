using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class address : Migration
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
                UPDATE leads SET lead_date = (created_at AT TIME ZONE 'UTC')::date WHERE lead_date IS NULL AND created_at IS NOT NULL;
                ALTER TABLE deals ADD COLUMN IF NOT EXISTS deal_amount numeric NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE leads DROP COLUMN IF EXISTS address;
                ALTER TABLE leads DROP COLUMN IF EXISTS lead_date;
                ALTER TABLE lead_histories DROP COLUMN IF EXISTS address;
                ALTER TABLE lead_histories DROP COLUMN IF EXISTS lead_date;
                ALTER TABLE deals DROP COLUMN IF EXISTS deal_amount;
                """);
        }
    }
}
