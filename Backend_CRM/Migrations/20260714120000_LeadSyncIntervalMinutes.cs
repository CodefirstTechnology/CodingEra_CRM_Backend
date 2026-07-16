using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <summary>
    /// Renames interval option duration from hours to minutes and converts existing values.
    /// </summary>
    public partial class LeadSyncIntervalMinutes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'lead_sync_interval_options'
                      AND column_name = 'hours'
                  ) THEN
                    ALTER INDEX IF EXISTS "IX_lead_sync_interval_options_hours"
                      RENAME TO "IX_lead_sync_interval_options_minutes";
                    ALTER TABLE lead_sync_interval_options RENAME COLUMN hours TO minutes;
                    -- Existing rows stored hours (1–24); convert once to minutes.
                    UPDATE lead_sync_interval_options
                    SET minutes = minutes * 60
                    WHERE minutes > 0 AND minutes < 60;
                  END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'lead_sync_interval_options'
                      AND column_name = 'minutes'
                  ) THEN
                    UPDATE lead_sync_interval_options
                    SET minutes = GREATEST(1, minutes / 60)
                    WHERE minutes >= 60;
                    ALTER TABLE lead_sync_interval_options RENAME COLUMN minutes TO hours;
                    ALTER INDEX IF EXISTS "IX_lead_sync_interval_options_minutes"
                      RENAME TO "IX_lead_sync_interval_options_hours";
                  END IF;
                END $$;
                """);
        }
    }
}
