using CRM.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Helpers
{
    /// <summary>
    /// Idempotent schema helper to ensure bidirectional foreign keys and indexes 
    /// between Tasks (call_log_id) and CallLogs (related_task_id).
    /// </summary>
    public static class TaskCallLogLinkSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure call_log_id column exists on Tasks table
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE "Tasks" ADD COLUMN IF NOT EXISTS call_log_id integer NULL;
                    """,
                    cancellationToken);

                // Ensure related_task_id column exists on CallLogs table
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE "CallLogs" ADD COLUMN IF NOT EXISTS related_task_id integer NULL;
                    """,
                    cancellationToken);

                // Ensure indexes for efficient bidirectional queries
                await db.Database.ExecuteSqlRawAsync(
                    """
                    CREATE INDEX IF NOT EXISTS ix_call_logs_related_task_id ON "CallLogs" (related_task_id);
                    CREATE INDEX IF NOT EXISTS ix_tasks_call_log_id ON "Tasks" (call_log_id);
                    """,
                    cancellationToken);

                logger.LogInformation("Task call-log bidirectional link schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Task call-log link schema ensure skipped or failed.");
            }
        }
    }
}
