using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("daily_sales_recognition_logs")]
    public class DailySalesRecognitionLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }

        [Column("executive_user_id")]
        public int? ExecutiveUserId { get; set; }

        [Column("executive_name")]
        [StringLength(150)]
        public string ExecutiveName { get; set; } = string.Empty;

        [Column("new_leads")]
        public int NewLeads { get; set; }

        [Column("customer_calls")]
        public int CustomerCalls { get; set; }

        [Column("meetings")]
        public int Meetings { get; set; }

        [Column("follow_ups")]
        public int FollowUps { get; set; }

        [Column("quotations")]
        public int Quotations { get; set; }

        [Column("lead_conversions")]
        public int LeadConversions { get; set; }

        [Column("orders_booked")]
        public int OrdersBooked { get; set; }

        [Column("payment_follow_ups")]
        public int PaymentFollowUps { get; set; }

        [Column("reporting_percentage", TypeName = "numeric(5,2)")]
        public decimal ReportingPercentage { get; set; } = 100m;

        [Column("team_support")]
        public int TeamSupport { get; set; } = 1;

        [Column("lead_quality_percentage", TypeName = "numeric(5,2)")]
        public decimal LeadQualityPercentage { get; set; } = 0m;

        [Column("daily_activity_score", TypeName = "numeric(5,2)")]
        public decimal DailyActivityScore { get; set; }

        [Column("business_result_score", TypeName = "numeric(5,2)")]
        public decimal BusinessResultScore { get; set; }

        [Column("overall_score", TypeName = "numeric(5,2)")]
        public decimal OverallScore { get; set; }

        [Column("recognition")]
        [StringLength(50)]
        public string Recognition { get; set; } = "Improve";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
