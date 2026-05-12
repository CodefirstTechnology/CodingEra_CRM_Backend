using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("CallLogs")]
    public class CallLog
    {
        [Key]
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

        public DateTime LastModified { get; set; }
    }
}