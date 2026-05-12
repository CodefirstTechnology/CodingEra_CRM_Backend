using System.ComponentModel.DataAnnotations;

namespace CRM.models
{
    public class CallLog
    {
        [Key]
        public int CallId { get; set; }

        public string Direction { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string ContactCompany { get; set; } = string.Empty;

        public DateTime CallStarted { get; set; }

        public int DurationMinutes { get; set; }

        public int DurationSeconds { get; set; }

        public string Outcome { get; set; } = string.Empty;

        public string? CallSummary { get; set; }
    }
}