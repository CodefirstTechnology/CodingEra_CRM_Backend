using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Helpers
{
    public static class SalesExecutiveReportSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Ensure task_type column on Tasks table (lowercased or quoted based on PostgreSQL table name)
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE "Tasks" ADD COLUMN IF NOT EXISTS task_type text NOT NULL DEFAULT 'Task';
                    """,
                    cancellationToken);

                // 2. Backfill historical tasks where title/description mentions meeting/demo
                await db.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE "Tasks"
                    SET task_type = 'Meeting'
                    WHERE (task_type = 'Task' OR task_type IS NULL OR task_type = '')
                      AND (
                        lower("TaskTitle") LIKE '%meeting%' OR 
                        lower("TaskTitle") LIKE '%demo%' OR 
                        lower("TaskDescription") LIKE '%meeting%' OR 
                        lower("TaskDescription") LIKE '%demo%'
                      );
                    """,
                    cancellationToken);

                // 3. Ensure is_positive column on lead_statuses table
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE lead_statuses ADD COLUMN IF NOT EXISTS is_positive boolean NOT NULL DEFAULT false;
                    """,
                    cancellationToken);

                // 4. Seed positive lead statuses based on name
                await db.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE lead_statuses
                    SET is_positive = true
                    WHERE lower(name) IN ('qualified', 'interested', 'proposal sent', 'demo scheduled', 'hot');
                    """,
                    cancellationToken);

                // 5. Create performance indexes for speed
                await db.Database.ExecuteSqlRawAsync(
                    """
                    CREATE INDEX IF NOT EXISTS ix_leads_owner_created ON leads (lead_owner_id, created_at, is_active);
                    CREATE INDEX IF NOT EXISTS ix_call_logs_created_by ON "CallLogs" (created_by, created_at, is_active);
                    CREATE INDEX IF NOT EXISTS ix_tasks_assignee_duedate ON "Tasks" (assignee_user_id, task_due_date, task_status, is_active);
                    CREATE INDEX IF NOT EXISTS ix_deals_owner_status ON deals (deal_owner_id, status, updated_at, is_active);
                    CREATE INDEX IF NOT EXISTS ix_quotations_creator ON quotations (created_by, status, created_at);
                    """,
                    cancellationToken);

                logger.LogInformation("Sales Executive Performance Report schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Sales Executive Performance Report schema ensure skipped or failed.");
            }
        }
    }
}
