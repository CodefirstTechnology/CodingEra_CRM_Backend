using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("CallLogs")]
    public class CallLog : IAuditableByUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CallId { get; set; }

        public string Direction { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string ContactCompany { get; set; } = string.Empty;

        public string ContactName { get; set; } = string.Empty;

        public DateTime CallStarted { get; set; }

        public int DurationMinutes { get; set; }

        public int DurationSeconds { get; set; }

        public string Outcome { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string? CallSummary { get; set; }

        public int? ContactId { get; set; }

        public int? RelatedLeadId { get; set; }

        public int? RelatedDealId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public DateTime LastModified { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}