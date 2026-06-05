using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class DealStatusPipelineFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE deal_statuses ADD COLUMN IF NOT EXISTS is_won boolean NOT NULL DEFAULT false;
                ALTER TABLE deal_statuses ADD COLUMN IF NOT EXISTS is_lost boolean NOT NULL DEFAULT false;
                UPDATE deal_statuses SET is_won = true
                WHERE COALESCE(is_won, false) = false
                  AND stage_key IS NOT NULL
                  AND lower(stage_key) = 'closed_won';
                UPDATE deal_statuses SET is_lost = true
                WHERE COALESCE(is_lost, false) = false
                  AND stage_key IS NOT NULL
                  AND lower(stage_key) = 'closed_lost';
                ALTER TABLE deal_statuses DROP COLUMN IF EXISTS stage_key;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE deal_statuses DROP COLUMN IF EXISTS is_won;
                ALTER TABLE deal_statuses DROP COLUMN IF EXISTS is_lost;
                ALTER TABLE deal_statuses ADD COLUMN IF NOT EXISTS stage_key character varying(64);
                """);
        }
    }
}
