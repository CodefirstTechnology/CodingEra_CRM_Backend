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
                await db.Database.ExecuteSqlRawAsync(
                    """
                    DO $$
                    BEGIN
                        -- Ensure call_log_id column exists on Tasks table
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'tasks') THEN
                            ALTER TABLE tasks ADD COLUMN IF NOT EXISTS call_log_id integer NULL;
                        END IF;
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Tasks') THEN
                            ALTER TABLE "Tasks" ADD COLUMN IF NOT EXISTS call_log_id integer NULL;
                        END IF;

                        -- Ensure related_task_id column exists on CallLogs table
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'call_logs') THEN
                            ALTER TABLE call_logs ADD COLUMN IF NOT EXISTS related_task_id integer NULL;
                        END IF;
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'CallLogs') THEN
                            ALTER TABLE "CallLogs" ADD COLUMN IF NOT EXISTS related_task_id integer NULL;
                        END IF;

                        -- Ensure indexes for efficient bidirectional queries
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'call_logs') THEN
                            CREATE INDEX IF NOT EXISTS ix_call_logs_related_task_id ON call_logs (related_task_id);
                        ELSIF EXISTS (SELECT FROM pg_tables WHERE tablename = 'CallLogs') THEN
                            CREATE INDEX IF NOT EXISTS ix_call_logs_related_task_id ON "CallLogs" (related_task_id);
                        END IF;

                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'tasks') THEN
                            CREATE INDEX IF NOT EXISTS ix_tasks_call_log_id ON tasks (call_log_id);
                        ELSIF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Tasks') THEN
                            CREATE INDEX IF NOT EXISTS ix_tasks_call_log_id ON "Tasks" (call_log_id);
                        END IF;
                    END $$;
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
