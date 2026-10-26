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
                // 1. Column additions with IF NOT EXISTS (case-tolerant)
                await db.Database.ExecuteSqlRawAsync(
                    """
                    DO $$
                    BEGIN
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'tasks') THEN
                            ALTER TABLE tasks ADD COLUMN IF NOT EXISTS task_type character varying(32) NOT NULL DEFAULT 'Task';
                        END IF;
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Tasks') THEN
                            ALTER TABLE "Tasks" ADD COLUMN IF NOT EXISTS task_type character varying(32) NOT NULL DEFAULT 'Task';
                        END IF;
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'lead_statuses') THEN
                            ALTER TABLE lead_statuses ADD COLUMN IF NOT EXISTS is_positive boolean NOT NULL DEFAULT false;
                        END IF;
                    END $$;
                    """,
                    cancellationToken);

                // 2. Backfill historical tasks where title/description mentions meeting/demo
                await db.Database.ExecuteSqlRawAsync(
                    """
                    DO $$
                    BEGIN
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'tasks') THEN
                            IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'tasks' AND column_name = 'task_title') THEN
                                UPDATE tasks
                                SET task_type = 'Meeting'
                                WHERE (task_type = 'Task' OR task_type IS NULL OR task_type = '')
                                  AND (
                                    lower(task_title) LIKE '%meeting%' OR 
                                    lower(task_title) LIKE '%demo%' OR 
                                    lower(coalesce(task_description, '')) LIKE '%meeting%' OR 
                                    lower(coalesce(task_description, '')) LIKE '%demo%'
                                  );
                            ELSIF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'tasks' AND column_name = 'TaskTitle') THEN
                                UPDATE tasks
                                SET task_type = 'Meeting'
                                WHERE (task_type = 'Task' OR task_type IS NULL OR task_type = '')
                                  AND (
                                    lower("TaskTitle") LIKE '%meeting%' OR 
                                    lower("TaskTitle") LIKE '%demo%' OR 
                                    lower(coalesce("TaskDescription", '')) LIKE '%meeting%' OR 
                                    lower(coalesce("TaskDescription", '')) LIKE '%demo%'
                                  );
                            END IF;
                        ELSIF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Tasks') THEN
                            IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'Tasks' AND column_name = 'TaskTitle') THEN
                                UPDATE "Tasks"
                                SET task_type = 'Meeting'
                                WHERE (task_type = 'Task' OR task_type IS NULL OR task_type = '')
                                  AND (
                                    lower("TaskTitle") LIKE '%meeting%' OR 
                                    lower("TaskTitle") LIKE '%demo%' OR 
                                    lower(coalesce("TaskDescription", '')) LIKE '%meeting%' OR 
                                    lower(coalesce("TaskDescription", '')) LIKE '%demo%'
                                  );
                            ELSIF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'Tasks' AND column_name = 'task_title') THEN
                                UPDATE "Tasks"
                                SET task_type = 'Meeting'
                                WHERE (task_type = 'Task' OR task_type IS NULL OR task_type = '')
                                  AND (
                                    lower(task_title) LIKE '%meeting%' OR 
                                    lower(task_title) LIKE '%demo%' OR 
                                    lower(coalesce(task_description, '')) LIKE '%meeting%' OR 
                                    lower(coalesce(task_description, '')) LIKE '%demo%'
                                  );
                            END IF;
                        END IF;
                    END $$;
                    """,
                    cancellationToken);

                // 3. Seed positive lead statuses based on name
                await db.Database.ExecuteSqlRawAsync(
                    """
                    DO $$
                    BEGIN
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'lead_statuses') THEN
                            UPDATE lead_statuses
                            SET is_positive = true
                            WHERE lower(name) IN ('qualified', 'interested', 'proposal sent', 'demo scheduled', 'hot');
                        END IF;
                    END $$;
                    """,
                    cancellationToken);

                // 4. Performance indexes for speed (safe & case-tolerant)
                await db.Database.ExecuteSqlRawAsync(
                    """
                    CREATE INDEX IF NOT EXISTS ix_leads_owner_created ON leads (lead_owner_id, created_at, is_active);
                    CREATE INDEX IF NOT EXISTS ix_deals_owner_status ON deals (deal_owner_id, status, updated_at, is_active);
                    CREATE INDEX IF NOT EXISTS ix_quotations_creator ON quotations (created_by, status, created_at);

                    DO $$
                    BEGIN
                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'tasks') THEN
                            IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'tasks' AND column_name = 'assignee_user_id') THEN
                                CREATE INDEX IF NOT EXISTS ix_tasks_assignee_duedate ON tasks (assignee_user_id, task_due_date, task_status, is_active);
                            ELSIF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'tasks' AND column_name = 'AssigneeUserId') THEN
                                CREATE INDEX IF NOT EXISTS ix_tasks_assignee_duedate ON tasks ("AssigneeUserId", "TaskDueDate", "TaskStatus", is_active);
                            END IF;
                        ELSIF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Tasks') THEN
                            IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'Tasks' AND column_name = 'AssigneeUserId') THEN
                                CREATE INDEX IF NOT EXISTS ix_tasks_assignee_duedate ON "Tasks" ("AssigneeUserId", "TaskDueDate", "TaskStatus", is_active);
                            ELSIF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'Tasks' AND column_name = 'assignee_user_id') THEN
                                CREATE INDEX IF NOT EXISTS ix_tasks_assignee_duedate ON "Tasks" (assignee_user_id, task_due_date, task_status, is_active);
                            END IF;
                        END IF;

                        IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'call_logs') THEN
                            CREATE INDEX IF NOT EXISTS ix_call_logs_created_by ON call_logs (created_by, created_at, is_active);
                        ELSIF EXISTS (SELECT FROM pg_tables WHERE tablename = 'CallLogs') THEN
                            CREATE INDEX IF NOT EXISTS ix_call_logs_created_by ON "CallLogs" (created_by, created_at, is_active);
                        END IF;
                    END $$;
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
