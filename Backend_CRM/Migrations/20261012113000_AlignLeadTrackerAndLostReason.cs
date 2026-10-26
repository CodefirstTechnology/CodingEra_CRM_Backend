using CRM.DATA;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <summary>
    /// Production EF Core Migration:
    /// 1. Adds lost_reason column (varchar 500) to leads table.
    /// 2. Updates vw_lead_tracker view with unified milestone completion, strict 1-to-1 lead cardinality,
    ///    actionable follow-up SLA, standardized quotation/order metrics, and unified lost reason projection.
    /// Executed automatically on startup via Database.MigrateAsync().
    /// </summary>
    public partial class AlignLeadTrackerAndLostReason : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Ensure prerequisite columns exist for vw_lead_tracker view and downstream queries
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Leads columns
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'leads') THEN
                        ALTER TABLE leads ADD COLUMN IF NOT EXISTS deal_amount numeric NULL;
                        ALTER TABLE leads ADD COLUMN IF NOT EXISTS lost_reason character varying(500) NULL;
                        ALTER TABLE leads ADD COLUMN IF NOT EXISTS is_qualified boolean NOT NULL DEFAULT false;
                    END IF;

                    -- Deals columns
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'deals') THEN
                        ALTER TABLE deals ADD COLUMN IF NOT EXISTS deal_amount numeric NULL;
                        ALTER TABLE deals ADD COLUMN IF NOT EXISTS lost_reason text NULL;
                    END IF;

                    -- Tasks table columns
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Tasks') THEN
                        ALTER TABLE ""Tasks"" ADD COLUMN IF NOT EXISTS task_type character varying(32) NOT NULL DEFAULT 'Task';
                        ALTER TABLE ""Tasks"" ADD COLUMN IF NOT EXISTS call_log_id integer NULL;
                    ELSIF EXISTS (SELECT FROM pg_tables WHERE tablename = 'tasks') THEN
                        ALTER TABLE tasks ADD COLUMN IF NOT EXISTS task_type character varying(32) NOT NULL DEFAULT 'Task';
                        ALTER TABLE tasks ADD COLUMN IF NOT EXISTS call_log_id integer NULL;
                    END IF;

                    -- CallLogs table columns
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'CallLogs') THEN
                        ALTER TABLE ""CallLogs"" ADD COLUMN IF NOT EXISTS related_task_id integer NULL;
                    ELSIF EXISTS (SELECT FROM pg_tables WHERE tablename = 'call_logs') THEN
                        ALTER TABLE call_logs ADD COLUMN IF NOT EXISTS related_task_id integer NULL;
                    END IF;

                    -- LeadStatuses table columns
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'lead_statuses') THEN
                        ALTER TABLE lead_statuses ADD COLUMN IF NOT EXISTS is_positive boolean NOT NULL DEFAULT false;
                        ALTER TABLE lead_statuses ADD COLUMN IF NOT EXISTS is_conversion_status boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;
            ");

            // 2. Drop and recreate production view vw_lead_tracker with unified rules
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_lead_tracker CASCADE;");
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_lead_tracker AS
                WITH lead_latest_tasks AS (
                    SELECT 
                        ""RelatedLeadId"" AS related_lead_id,
                        COALESCE(
                            MAX(CASE 
                                WHEN (LOWER(COALESCE(task_type, '')) IN ('meeting', 'demo', 'visit') 
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%meet%' 
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%demo%'
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%visit%'
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%discussion%'
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%presentation%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%meet%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%demo%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%visit%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%discussion%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%presentation%')
                                     AND LOWER(COALESCE(""TaskStatus"", '')) IN ('done', 'completed') 
                                THEN COALESCE(""TaskDueDate"", created_at) 
                            END),
                            MAX(CASE 
                                WHEN (LOWER(COALESCE(task_type, '')) IN ('meeting', 'demo', 'visit') 
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%meet%' 
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%demo%'
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%visit%'
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%discussion%'
                                      OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%presentation%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%meet%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%demo%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%visit%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%discussion%'
                                      OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%presentation%')
                                THEN COALESCE(""TaskDueDate"", created_at) 
                            END)
                        ) AS last_meeting_date,
                        MAX(CASE 
                            WHEN (LOWER(COALESCE(task_type, '')) IN ('meeting', 'demo', 'visit') 
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%meet%' 
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%demo%'
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%visit%'
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%discussion%'
                                  OR LOWER(COALESCE(""TaskTitle"", '')) LIKE '%presentation%'
                                  OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%meet%'
                                  OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%demo%'
                                  OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%visit%'
                                  OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%discussion%'
                                  OR LOWER(COALESCE(""TaskDescription"", '')) LIKE '%presentation%')
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
                        END) AS next_followup_date,
                        MAX(CASE 
                            WHEN LOWER(COALESCE(""TaskStatus"", '')) IN ('todo', 'pending', 'open') 
                            THEN ""TaskId"" 
                        END) AS pending_task_id
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
                    
                    -- Rule 3: Unified Qualified Milestone (once qualified or converted to deal, stays true)
                    CASE 
                        WHEN COALESCE(l.is_qualified, false) = true
                             OR COALESCE(ls.is_positive, false) = true 
                             OR COALESCE(ls.is_conversion_status, false) = true
                             OR LOWER(COALESCE(ls.name, '')) IN ('qualified', 'interested', 'demo scheduled', 'proposal sent', 'hot', 'converted', 'moved to deal') 
                             OR d.deal_id IS NOT NULL
                             OR q.quotation_date IS NOT NULL
                             OR COALESCE(q.total_quotation_value, 0) > 0
                             OR EXISTS (
                                 SELECT 1 FROM lead_histories lh
                                 JOIN lead_statuses lhs ON lh.lead_status_id = lhs.id
                                 WHERE lh.lead_id = l.id 
                                   AND (COALESCE(lhs.is_positive, false) = true 
                                        OR COALESCE(lhs.is_conversion_status, false) = true 
                                        OR LOWER(COALESCE(lhs.name, '')) IN ('qualified', 'interested', 'demo scheduled', 'proposal sent', 'hot', 'converted', 'moved to deal'))
                             )
                             OR EXISTS (
                                 SELECT 1 FROM activity_logs al
                                 WHERE al.entity_type = 'lead' AND al.entity_id = l.id
                                   AND (
                                       LOWER(COALESCE(al.new_value, '')) IN ('qualified', 'converted', 'moved to deal')
                                       OR LOWER(COALESCE(al.old_value, '')) IN ('qualified', 'converted', 'moved to deal')
                                       OR LOWER(COALESCE(al.message, '')) LIKE '%qualified%'
                                       OR LOWER(COALESCE(al.message, '')) LIKE '%converted%'
                                   )
                             )
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
                    t.pending_task_id,
                    
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
                    
                    -- Rule 5: Unified Lost Reason (Deal reason takes precedence, fallback to Lead reason)
                    COALESCE(NULLIF(d.lost_reason, ''), NULLIF(l.lost_reason, ''), '') AS lost_reason,
                    
                    -- Rule 4: Dynamic Days Open
                    -- If nothing happens on that lead (no activity, no calls, no tasks, no status change from initial 'New'): shows 0 days
                    -- If there is ANY activity on the lead (even if status changes to unqualified on day 0): shows at least 1 day open
                    CASE 
                        WHEN NOT (
                            COALESCE(l.is_qualified, false) = true
                            OR COALESCE(ls.is_positive, false) = true 
                            OR COALESCE(ls.is_conversion_status, false) = true
                            OR LOWER(COALESCE(ls.name, '')) NOT IN ('', 'new', 'open')
                            OR l.lead_status_id NOT IN (1)
                            OR t.related_lead_id IS NOT NULL
                            OR EXISTS (SELECT 1 FROM ""CallLogs"" cl WHERE cl.""RelatedLeadId"" = l.id)
                            OR EXISTS (SELECT 1 FROM notes n WHERE (n.related_lead_id = l.id OR (n.related_type = 'lead' AND n.record_id = l.id)) AND n.is_active = true)
                            OR EXISTS (SELECT 1 FROM lead_histories lh WHERE lh.lead_id = l.id)
                            OR EXISTS (SELECT 1 FROM activity_logs al WHERE al.entity_type = 'lead' AND al.entity_id = l.id AND al.action_type NOT IN ('created', 'lead_created'))
                            OR d.deal_id IS NOT NULL
                            OR q.quotation_date IS NOT NULL
                            OR (l.updated_at IS NOT NULL AND l.created_at IS NOT NULL AND l.updated_at > l.created_at + INTERVAL '10 seconds')
                        ) THEN 0
                        
                        WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%won%' 
                             OR LOWER(COALESCE(d.status, ls.name, '')) LIKE '%lost%' 
                             OR l.lead_status_id IN (4, 6)
                             OR LOWER(COALESCE(ls.name, '')) IN ('unqualified', 'junk', 'lost')
                        THEN GREATEST(1, (COALESCE(d.order_date, l.updated_at, CURRENT_TIMESTAMP)::date - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                        
                        ELSE GREATEST(1, (CURRENT_DATE - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                    END AS days_open,
                    
                    CASE 
                        WHEN (
                            CASE 
                                WHEN NOT (
                                    COALESCE(l.is_qualified, false) = true
                                    OR COALESCE(ls.is_positive, false) = true 
                                    OR COALESCE(ls.is_conversion_status, false) = true
                                    OR LOWER(COALESCE(ls.name, '')) NOT IN ('', 'new', 'open')
                                    OR l.lead_status_id NOT IN (1)
                                    OR t.related_lead_id IS NOT NULL
                                    OR EXISTS (SELECT 1 FROM ""CallLogs"" cl WHERE cl.""RelatedLeadId"" = l.id)
                                    OR EXISTS (SELECT 1 FROM notes n WHERE (n.related_lead_id = l.id OR (n.related_type = 'lead' AND n.record_id = l.id)) AND n.is_active = true)
                                    OR EXISTS (SELECT 1 FROM lead_histories lh WHERE lh.lead_id = l.id)
                                    OR EXISTS (SELECT 1 FROM activity_logs al WHERE al.entity_type = 'lead' AND al.entity_id = l.id AND al.action_type NOT IN ('created', 'lead_created'))
                                    OR d.deal_id IS NOT NULL
                                    OR q.quotation_date IS NOT NULL
                                    OR (l.updated_at IS NOT NULL AND l.created_at IS NOT NULL AND l.updated_at > l.created_at + INTERVAL '10 seconds')
                                ) THEN 0
                                WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%won%' 
                                     OR LOWER(COALESCE(d.status, ls.name, '')) LIKE '%lost%' 
                                     OR l.lead_status_id IN (4, 6)
                                     OR LOWER(COALESCE(ls.name, '')) IN ('unqualified', 'junk', 'lost')
                                THEN GREATEST(1, (COALESCE(d.order_date, l.updated_at, CURRENT_TIMESTAMP)::date - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                                ELSE GREATEST(1, (CURRENT_DATE - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                            END
                        ) <= 2 THEN '0-2 Days'
                        WHEN (
                            CASE 
                                WHEN NOT (
                                    COALESCE(l.is_qualified, false) = true
                                    OR COALESCE(ls.is_positive, false) = true 
                                    OR COALESCE(ls.is_conversion_status, false) = true
                                    OR LOWER(COALESCE(ls.name, '')) NOT IN ('', 'new', 'open')
                                    OR l.lead_status_id NOT IN (1)
                                    OR t.related_lead_id IS NOT NULL
                                    OR EXISTS (SELECT 1 FROM ""CallLogs"" cl WHERE cl.""RelatedLeadId"" = l.id)
                                    OR EXISTS (SELECT 1 FROM notes n WHERE (n.related_lead_id = l.id OR (n.related_type = 'lead' AND n.record_id = l.id)) AND n.is_active = true)
                                    OR EXISTS (SELECT 1 FROM lead_histories lh WHERE lh.lead_id = l.id)
                                    OR EXISTS (SELECT 1 FROM activity_logs al WHERE al.entity_type = 'lead' AND al.entity_id = l.id AND al.action_type NOT IN ('created', 'lead_created'))
                                    OR d.deal_id IS NOT NULL
                                    OR q.quotation_date IS NOT NULL
                                    OR (l.updated_at IS NOT NULL AND l.created_at IS NOT NULL AND l.updated_at > l.created_at + INTERVAL '10 seconds')
                                ) THEN 0
                                WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%won%' 
                                     OR LOWER(COALESCE(d.status, ls.name, '')) LIKE '%lost%' 
                                     OR l.lead_status_id IN (4, 6)
                                     OR LOWER(COALESCE(ls.name, '')) IN ('unqualified', 'junk', 'lost')
                                THEN GREATEST(1, (COALESCE(d.order_date, l.updated_at, CURRENT_TIMESTAMP)::date - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                                ELSE GREATEST(1, (CURRENT_DATE - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                            END
                        ) <= 7 THEN '3-7 Days'
                        WHEN (
                            CASE 
                                WHEN NOT (
                                    COALESCE(l.is_qualified, false) = true
                                    OR COALESCE(ls.is_positive, false) = true 
                                    OR COALESCE(ls.is_conversion_status, false) = true
                                    OR LOWER(COALESCE(ls.name, '')) NOT IN ('', 'new', 'open')
                                    OR l.lead_status_id NOT IN (1)
                                    OR t.related_lead_id IS NOT NULL
                                    OR EXISTS (SELECT 1 FROM ""CallLogs"" cl WHERE cl.""RelatedLeadId"" = l.id)
                                    OR EXISTS (SELECT 1 FROM notes n WHERE (n.related_lead_id = l.id OR (n.related_type = 'lead' AND n.record_id = l.id)) AND n.is_active = true)
                                    OR EXISTS (SELECT 1 FROM lead_histories lh WHERE lh.lead_id = l.id)
                                    OR EXISTS (SELECT 1 FROM activity_logs al WHERE al.entity_type = 'lead' AND al.entity_id = l.id AND al.action_type NOT IN ('created', 'lead_created'))
                                    OR d.deal_id IS NOT NULL
                                    OR q.quotation_date IS NOT NULL
                                    OR (l.updated_at IS NOT NULL AND l.created_at IS NOT NULL AND l.updated_at > l.created_at + INTERVAL '10 seconds')
                                ) THEN 0
                                WHEN LOWER(COALESCE(d.status, ls.name, '')) LIKE '%won%' 
                                     OR LOWER(COALESCE(d.status, ls.name, '')) LIKE '%lost%' 
                                     OR l.lead_status_id IN (4, 6)
                                     OR LOWER(COALESCE(ls.name, '')) IN ('unqualified', 'junk', 'lost')
                                THEN GREATEST(1, (COALESCE(d.order_date, l.updated_at, CURRENT_TIMESTAMP)::date - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                                ELSE GREATEST(1, (CURRENT_DATE - COALESCE(l.created_at, CURRENT_TIMESTAMP)::date))
                            END
                        ) <= 14 THEN '8-14 Days'
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
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_lead_tracker;");
            migrationBuilder.DropColumn(
                name: "lost_reason",
                table: "leads");
        }
    }
}
