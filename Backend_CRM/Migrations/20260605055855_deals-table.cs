using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class dealstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columns may already exist from DealPipelineStageSeed (startup SQL).
            migrationBuilder.Sql(
                """
                ALTER TABLE deals ADD COLUMN IF NOT EXISTS lost_reason text NOT NULL DEFAULT '';
                ALTER TABLE deal_statuses ADD COLUMN IF NOT EXISTS sort_order integer NOT NULL DEFAULT 0;
                ALTER TABLE deal_statuses ADD COLUMN IF NOT EXISTS is_won boolean NOT NULL DEFAULT false;
                ALTER TABLE deal_statuses ADD COLUMN IF NOT EXISTS is_lost boolean NOT NULL DEFAULT false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE deals DROP COLUMN IF EXISTS lost_reason;
                ALTER TABLE deal_statuses DROP COLUMN IF EXISTS sort_order;
                ALTER TABLE deal_statuses DROP COLUMN IF EXISTS is_won;
                ALTER TABLE deal_statuses DROP COLUMN IF EXISTS is_lost;
                """);
        }
    }
}
