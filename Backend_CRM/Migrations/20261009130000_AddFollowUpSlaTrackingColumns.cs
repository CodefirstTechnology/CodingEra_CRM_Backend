using CRM.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <summary>
    /// Production EF Core Migration: Adds SLA tracking columns and partial performance indexes to "Tasks" table.
    /// Executed automatically on API startup via DbContext.Database.MigrateAsync().
    /// </summary>
    [DbContext(typeof(TaskDbcontext))]
    [Migration("20261009130000_AddFollowUpSlaTrackingColumns")]
    public partial class AddFollowUpSlaTrackingColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Tasks"" 
                ADD COLUMN IF NOT EXISTS ""first_due_alert_sent_at"" TIMESTAMPTZ NULL,
                ADD COLUMN IF NOT EXISTS ""last_repetition_alert_sent_at"" TIMESTAMPTZ NULL,
                ADD COLUMN IF NOT EXISTS ""is_escalated_to_admin"" BOOLEAN NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS ""escalated_to_admin_at"" TIMESTAMPTZ NULL;

                CREATE INDEX IF NOT EXISTS ""IX_Tasks_SLA_Executive_Alerts""
                ON ""Tasks"" (""AssigneeUserId"", ""TaskDueDate"", ""last_repetition_alert_sent_at"")
                WHERE ""is_active"" = true 
                  AND ""RelatedLeadId"" IS NOT NULL 
                  AND ""TaskStatus"" NOT IN ('Completed', 'Done', 'Canceled');

                CREATE INDEX IF NOT EXISTS ""IX_Tasks_SLA_Admin_Escalations""
                ON ""Tasks"" (""TaskDueDate"", ""is_escalated_to_admin"")
                WHERE ""is_active"" = true 
                  AND ""RelatedLeadId"" IS NOT NULL 
                  AND ""TaskStatus"" NOT IN ('Completed', 'Done', 'Canceled')
                  AND ""is_escalated_to_admin"" = false;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_Tasks_SLA_Executive_Alerts"";
                DROP INDEX IF EXISTS ""IX_Tasks_SLA_Admin_Escalations"";

                ALTER TABLE ""Tasks""
                DROP COLUMN IF EXISTS ""first_due_alert_sent_at"",
                DROP COLUMN IF EXISTS ""last_repetition_alert_sent_at"",
                DROP COLUMN IF EXISTS ""is_escalated_to_admin"",
                DROP COLUMN IF EXISTS ""escalated_to_admin_at"";
            ");
        }
    }
}
