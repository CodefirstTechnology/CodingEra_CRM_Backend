using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("vw_lead_tracker")]
    public class LeadTrackerRow
    {
        [Column("lead_id_num")]
        public int LeadIdNum { get; set; }

        [Column("lead_id_formatted")]
        public string LeadIdFormatted { get; set; } = string.Empty;

        [Column("lead_date")]
        public DateTime? LeadDate { get; set; }

        [Column("sales_executive")]
        public string SalesExecutive { get; set; } = string.Empty;

        [Column("sales_executive_user_id")]
        public int? SalesExecutiveUserId { get; set; }

        [Column("lead_source")]
        public string LeadSource { get; set; } = string.Empty;

        [Column("customer_company_name")]
        public string CustomerCompanyName { get; set; } = string.Empty;

        [Column("contact_person")]
        public string ContactPerson { get; set; } = string.Empty;

        [Column("mobile")]
        public string Mobile { get; set; } = string.Empty;

        [Column("requirement_product")]
        public string RequirementProduct { get; set; } = string.Empty;

        [Column("estimated_lead_value")]
        public decimal EstimatedLeadValue { get; set; }

        [Column("first_contact_date")]
        public DateTime? FirstContactDate { get; set; }

        [Column("is_contacted")]
        public bool IsContacted { get; set; }

        [Column("is_qualified")]
        public bool IsQualified { get; set; }

        [Column("meeting_date")]
        public DateTime? MeetingDate { get; set; }

        [Column("is_meeting_done")]
        public bool IsMeetingDone { get; set; }

        [Column("quotation_date")]
        public DateTime? QuotationDate { get; set; }

        [Column("quotation_value")]
        public decimal QuotationValue { get; set; }

        [Column("last_followup_date")]
        public DateTime? LastFollowupDate { get; set; }

        [Column("next_followup_date")]
        public DateTime? NextFollowupDate { get; set; }

        [Column("current_stage")]
        public string CurrentStage { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("expected_close_date")]
        public DateTime? ExpectedOrderDate { get; set; }

        [Column("order_date")]
        public DateTime? OrderDate { get; set; }

        [Column("order_value")]
        public decimal OrderValue { get; set; }

        [Column("lost_reason")]
        public string? LostReason { get; set; }

        // COMPUTED & DERIVED COLUMNS
        [Column("days_open")]
        public int DaysOpen { get; set; }

        [Column("ageing_bucket")]
        public string AgeingBucket { get; set; } = string.Empty;

        [Column("followup_due_status")]
        public string FollowupDueStatus { get; set; } = string.Empty;

        [Column("month_label")]
        public string MonthLabel { get; set; } = string.Empty;

        [Column("lead_year")]
        public int LeadYear { get; set; }

        [Column("lead_month_num")]
        public int LeadMonthNum { get; set; }
    }
}
