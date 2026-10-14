using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.DATA
{
    /// <summary>
    /// Extension methods for safe, idempotent, and fault-tolerant database initialization on startup.
    /// Handles EF Core migrations, custom PostgreSQL schema DDLs, performance indexes, and analytical views.
    /// </summary>
    public static class DatabaseInitializerExtensions
    {
        public static async Task InitializeDatabaseAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            await scope.ServiceProvider.InitializeDatabaseAsync();
        }

        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<TaskDbcontext>>();

            try
            {
                var dbContext = serviceProvider.GetRequiredService<TaskDbcontext>();

                logger.LogInformation("[DatabaseInitializer] Starting startup database initialization...");

                // 1. Safe EF Core migrations execution with retry logic
                await ApplyMigrationsWithRetryAsync(dbContext, logger);

                // 2. Performance indexes on primary tables
                await EnsurePerformanceIndexesAsync(dbContext, logger);

                // 3. SLA columns and partial indexes on Tasks table
                await EnsureTaskSlaSchemaAndIndexesAsync(dbContext, logger);

                // 4. Daily sales recognition logs table schema and indexes
                await EnsureDailySalesRecognitionSchemaAsync(dbContext, logger);

                // 5. Unified vw_lead_tracker analytical view
                await EnsureLeadTrackerViewAsync(dbContext, logger);

                logger.LogInformation("[DatabaseInitializer] Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                // Graceful fallback: Do not crash the application boot sequence
                logger.LogError(ex, "[DatabaseInitializer] Unhandled exception occurred during database initialization. Application will continue booting.");
            }
        }

        private static async Task ApplyMigrationsWithRetryAsync(TaskDbcontext dbContext, ILogger logger, int maxRetries = 3)
        {
            int attempt = 0;
            while (attempt < maxRetries)
            {
                try
                {
                    attempt++;
                    logger.LogInformation("[DatabaseInitializer] Applying pending EF Core migrations (Attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("[DatabaseInitializer] EF Core migrations applied successfully.");
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    logger.LogWarning(ex, "[DatabaseInitializer] EF Core migration attempt {Attempt} failed. Retrying in 2 seconds...", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[DatabaseInitializer] EF Core migration failed after {MaxRetries} attempts. Continuing startup...", maxRetries);
                    return;
                }
            }
        }

        private static async Task EnsurePerformanceIndexesAsync(TaskDbcontext dbContext, ILogger logger)
        {
            logger.LogInformation("[DatabaseInitializer] Ensuring performance indexes exist...");

            var indexStatements = new[]
            {
                @"CREATE INDEX IF NOT EXISTS ""IX_leads_is_active_lead_owner_id_created_at"" ON leads (is_active, lead_owner_id, created_at DESC);",
                @"CREATE INDEX IF NOT EXISTS ""IX_deals_org_id_is_active_created_at"" ON deals (organization_id, is_active, created_at DESC) WHERE organization_id IS NOT NULL;",
                @"CREATE INDEX IF NOT EXISTS ""IX_quotations_deal_id_status"" ON quotations (deal_id, status) WHERE deal_id IS NOT NULL;",
                @"CREATE INDEX IF NOT EXISTS ""IX_Tasks_related_lead_id_task_status_task_due_date"" ON ""Tasks"" (""RelatedLeadId"", ""TaskStatus"", ""TaskDueDate"") WHERE ""RelatedLeadId"" IS NOT NULL;"
            };

            foreach (var sql in indexStatements)
            {
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(sql);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[DatabaseInitializer] Failed to apply performance index statement: {Sql}", sql);
                }
            }
        }

        private static async Task EnsureTaskSlaSchemaAndIndexesAsync(TaskDbcontext dbContext, ILogger logger)
        {
            logger.LogInformation("[DatabaseInitializer] Ensuring Task SLA schema columns and partial indexes exist...");

            const string slaSchemaSql = @"
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
            ";

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(slaSchemaSql);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[DatabaseInitializer] Failed to apply Task SLA schema or indexes.");
            }
        }

        private static async Task EnsureDailySalesRecognitionSchemaAsync(TaskDbcontext dbContext, ILogger logger)
        {
            logger.LogInformation("[DatabaseInitializer] Ensuring daily_sales_recognition_logs table and indexes exist...");

            const string tableSql = @"
                CREATE TABLE IF NOT EXISTS daily_sales_recognition_logs (
                    id SERIAL PRIMARY KEY,
                    date TIMESTAMPTZ NOT NULL,
                    executive_user_id INT NULL,
                    executive_name VARCHAR(150) NOT NULL,
                    new_leads INT NOT NULL DEFAULT 0,
                    customer_calls INT NOT NULL DEFAULT 0,
                    meetings INT NOT NULL DEFAULT 0,
                    follow_ups INT NOT NULL DEFAULT 0,
                    quotations INT NOT NULL DEFAULT 0,
                    lead_conversions INT NOT NULL DEFAULT 0,
                    orders_booked INT NOT NULL DEFAULT 0,
                    payment_follow_ups INT NOT NULL DEFAULT 0,
                    reporting_percentage NUMERIC(5,2) NOT NULL DEFAULT 100.0,
                    team_support INT NOT NULL DEFAULT 1,
                    lead_quality_percentage NUMERIC(5,2) NOT NULL DEFAULT 0.0,
                    daily_activity_score NUMERIC(5,2) NOT NULL DEFAULT 0.0,
                    business_result_score NUMERIC(5,2) NOT NULL DEFAULT 0.0,
                    overall_score NUMERIC(5,2) NOT NULL DEFAULT 0.0,
                    recognition VARCHAR(50) NOT NULL DEFAULT 'Improve',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS ""IX_daily_sales_recognition_logs_date"" ON daily_sales_recognition_logs (date DESC);
                CREATE INDEX IF NOT EXISTS ""IX_daily_sales_recognition_logs_executive"" ON daily_sales_recognition_logs (executive_name);
            ";

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(tableSql);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[DatabaseInitializer] Failed to ensure daily_sales_recognition_logs schema.");
            }
        }

        private static async Task EnsureLeadTrackerViewAsync(TaskDbcontext dbContext, ILogger logger)
        {
            logger.LogInformation("[DatabaseInitializer] Updating vw_lead_tracker view...");

            const string viewSql = @"
                DROP VIEW IF EXISTS vw_lead_tracker CASCADE;

                CREATE OR REPLACE VIEW vw_lead_tracker AS
                WITH lead_latest_tasks AS (
                    SELECT 
                        ""RelatedLeadId"" AS related_lead_id,
                        MAX(CASE 
                            WHEN (LOWER(COALESCE(task_type, '')) = 'meeting' 
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%meet%' 
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%demo%'
                                  OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%meet%'
                                  OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%demo%')
                                 AND LOWER(COALESCE(""TaskStatus"", '')) IN ('done', 'completed') 
                            THEN created_at 
                        END) AS last_meeting_date,
                        MAX(CASE 
                            WHEN (LOWER(COALESCE(task_type, '')) = 'meeting' 
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%meet%' 
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%demo%'
                                  OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%meet%'
                                  OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%demo%')
                                 AND LOWER(COALESCE(""TaskStatus"", '')) IN ('done', 'completed') 
                            THEN 1 ELSE 0 
                        END) AS is_meeting_done,
                        MAX(CASE 
                            WHEN (LOWER(COALESCE(task_type, '')) = 'call' 
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%call%')
                                 AND LOWER(COALESCE(""TaskStatus"", '')) IN ('done', 'completed') 
                            THEN 1 ELSE 0 
                        END) AS is_call_task_done,
                        MAX(created_at) AS last_followup_date,
                        MIN(CASE 
                            WHEN LOWER(COALESCE(""TaskStatus"", '')) IN ('todo', 'pending', 'open') 
                                 AND ""TaskDueDate"" >= CURRENT_TIMESTAMP 
                            THEN ""TaskDueDate"" 
                        END) AS next_followup_date
                    FROM ""Tasks""
                    WHERE ""RelatedLeadId"" IS NOT NULL AND is_active = true
                    GROUP BY ""RelatedLeadId""
                ),
                lead_deals_summary AS (
                    SELECT DISTINCT ON (COALESCE(d.organization_id, d.contact_id, d.id))
                        d.id AS deal_id,
                        d.organization_id,
                        d.contact_id,
                        d.status AS current_stage,
                        d.status,
                        d.created_at AS order_date,
                        d.next_follow_up_date AS expected_close_date,
                        COALESCE(d.deal_amount, 0) AS order_value,
                        d.lost_reason
                    FROM deals d
                    WHERE d.is_active = true
                    ORDER BY COALESCE(d.organization_id, d.contact_id, d.id), d.created_at DESC, d.id DESC
                ),
                lead_quotations_summary AS (
                    SELECT 
                        q.deal_id,
                        MIN(q.quotation_date) AS quotation_date,
                        SUM(COALESCE(q.grand_total, 0)) AS total_quotation_value
                    FROM quotations q
                    WHERE q.status NOT IN ('Draft', 'Cancelled') AND q.deal_id IS NOT NULL
                    GROUP BY q.deal_id
                )
                SELECT 
                    l.id AS lead_id_num,
                    CONCAT('L', LPAD(l.id::text, 4, '0')) AS lead_id_formatted,
                    COALESCE(l.created_at, CURRENT_TIMESTAMP) AS lead_date,
                    COALESCE(u.full_name, 'Unassigned') AS sales_executive,
                    u.id AS sales_executive_user_id,
                    COALESCE(l.lead_source, 'Direct') AS lead_source,
                    COALESCE(o.name, CONCAT(COALESCE(l.first_name, ''), ' ', COALESCE(l.last_name, ''))) AS customer_company_name,
                    TRIM(CONCAT(COALESCE(l.first_name, ''), ' ', COALESCE(l.last_name, ''))) AS contact_person,
                    COALESCE(l.mobile, '') AS mobile,
                    COALESCE(l.notes, '') AS requirement_product,
                    COALESCE(l.deal_amount, 0) AS estimated_lead_value,
                    
                    COALESCE(l.created_at, CURRENT_TIMESTAMP) AS first_contact_date,
                    
                    -- Rule 3: Unified Contacted Milestone
                    CASE 
                        WHEN l.lead_status_id = 2 
                             OR EXISTS (SELECT 1 FROM ""CallLogs"" cl WHERE cl.""RelatedLeadId"" = l.id) 
                             OR COALESCE(t.is_call_task_done, 0) = 1 
                        THEN true 
                        ELSE false 
                    END AS is_contacted,
                    
                    -- Rule 3: Unified Qualified Milestone
                    CASE 
                        WHEN COALESCE(ls.is_positive, false) = true 
                             OR LOWER(COALESCE(ls.name, '')) IN ('qualified', 'interested', 'demo scheduled', 'proposal sent', 'hot') 
                        THEN true 
                        ELSE false 
                    END AS is_qualified,
                    
                    -- Rule 3: Unified Meeting Done Milestone
                    t.last_meeting_date AS meeting_date,
                    CASE WHEN COALESCE(t.is_meeting_done, 0) = 1 THEN true ELSE false END AS is_meeting_done,
                    
                    q.quotation_date,
                    COALESCE(q.total_quotation_value, 0) AS quotation_value,
                    
                    t.last_followup_date,
                    t.next_followup_date,
                    
                    COALESCE(d.current_stage, ls.name, 'New Lead') AS current_stage,
                    CASE 
                        WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%won%' THEN 'Won'
                        WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%lost%' 
                             OR l.lead_status_id IN (4, 6) 
                             OR LOWER(COALESCE(ls.name, '')) IN ('unqualified', 'junk', 'lost') 
                        THEN 'Lost'
                        ELSE 'Open'
                    END AS status,
                    d.expected_close_date,
                    d.order_date,
                    COALESCE(d.order_value, 0) AS order_value,
                    
                    -- Rule 5: Unified Lost Reason
                    COALESCE(NULLIF(d.lost_reason, ''), NULLIF(l.lost_reason, ''), '') AS lost_reason,
                    
                    CASE 
                        WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%won%' 
                             OR LOWER(COALESCE(d.status, ls.name, '')) LIKE '%lost%' 
                             OR l.lead_status_id IN (4, 6)
                             OR LOWER(COALESCE(ls.name, '')) IN ('unqualified', 'junk', 'lost')
                        THEN GREATEST(0, (COALESCE(d.order_date, l.updated_at, CURRENT_TIMESTAMP)::date - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                        ELSE GREATEST(0, (CURRENT_DATE - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                    END AS days_open,
                    
                    CASE 
                        WHEN (CASE WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%won%' OR LOWER(COALESCE(d.status, ls.name, '')) LIKE '%lost%' OR l.lead_status_id IN (4, 6) OR LOWER(COALESCE(ls.name, '')) IN ('unqualified', 'junk', 'lost') THEN GREATEST(0, (COALESCE(d.order_date, l.updated_at, CURRENT_TIMESTAMP)::date - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date)) ELSE GREATEST(0, (CURRENT_DATE - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date)) END) <= 2 THEN '0-2 Days'
                        WHEN (CASE WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%won%' OR LOWER(COALESCE(d.status, ls.name, '')) LIKE '%lost%' OR l.lead_status_id IN (4, 6) OR LOWER(COALESCE(ls.name, '')) IN ('unqualified', 'junk', 'lost') THEN GREATEST(0, (COALESCE(d.order_date, l.updated_at, CURRENT_TIMESTAMP)::date - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date)) ELSE GREATEST(0, (CURRENT_DATE - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date)) END) <= 7 THEN '3-7 Days'
                        WHEN (CASE WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%won%' OR LOWER(COALESCE(d.status, ls.name, '')) LIKE '%lost%' OR l.lead_status_id IN (4, 6) OR LOWER(COALESCE(ls.name, '')) IN ('unqualified', 'junk', 'lost') THEN GREATEST(0, (COALESCE(d.order_date, l.updated_at, CURRENT_TIMESTAMP)::date - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date)) ELSE GREATEST(0, (CURRENT_DATE - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date)) END) <= 14 THEN '8-14 Days'
                        ELSE '15+ Days'
                    END AS ageing_bucket,
                    
                    CASE 
                        WHEN t.next_followup_date IS NULL THEN 'NO SCHEDULE'
                        WHEN t.next_followup_date::date < CURRENT_DATE THEN 'OVERDUE'
                        WHEN t.next_followup_date::date = CURRENT_DATE THEN 'DUE TODAY'
                        ELSE 'UPCOMING'
                    END AS followup_due_status,
                    
                    TO_CHAR(COALESCE(l.created_at, CURRENT_TIMESTAMP), 'Mon-YYYY') AS month_label,
                    EXTRACT(YEAR FROM COALESCE(l.created_at, CURRENT_TIMESTAMP))::integer AS lead_year,
                    EXTRACT(MONTH FROM COALESCE(l.created_at, CURRENT_TIMESTAMP))::integer AS lead_month_num

                FROM leads l
                LEFT JOIN users u ON l.lead_owner_id = u.id
                LEFT JOIN organizations o ON l.organization_id = o.id
                LEFT JOIN lead_statuses ls ON l.lead_status_id = ls.id
                LEFT JOIN lead_latest_tasks t ON t.related_lead_id = l.id
                LEFT JOIN lead_deals_summary d ON (l.organization_id IS NOT NULL AND d.organization_id = l.organization_id)
                LEFT JOIN lead_quotations_summary q ON q.deal_id = d.deal_id
                WHERE l.is_active = true;
            ";

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(viewSql);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[DatabaseInitializer] Failed to create or replace vw_lead_tracker view.");
            }
        }
    }
}
