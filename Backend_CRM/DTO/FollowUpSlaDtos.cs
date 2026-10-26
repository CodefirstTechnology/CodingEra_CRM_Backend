using System;

namespace CRM.DTO
{
    public class FollowUpPopupEventDto
    {
        public int TaskId { get; set; }
        public int LeadId { get; set; }
        public string LeadFormattedId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string TaskTitle { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int MinutesOverdue { get; set; }
        public bool IsInitialDue { get; set; }
    }

    public class FollowUpAdminEscalationDto
    {
        public int TaskId { get; set; }
        public int LeadId { get; set; }
        public string LeadFormattedId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string SalesExecutiveName { get; set; } = string.Empty;
        public int SalesExecutiveUserId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int MinutesOverdue { get; set; }
        public string Severity { get; set; } = "CRITICAL";
        public DateTime EscalatedAt { get; set; }
    }

    public class CompleteFollowUpRequestDto
    {
        public int TaskId { get; set; }
        public string? Notes { get; set; }
        public int CallDurationSeconds { get; set; } = 60;
        public bool IsCallLogged { get; set; } = true;
    }

    public class RescheduleFollowUpRequestDto
    {
        public int TaskId { get; set; }
        public DateTime NewDueDate { get; set; }
        public string RescheduleReason { get; set; } = string.Empty;
    }

    public class DismissFollowUpPopupDto
    {
        public int TaskId { get; set; }
        public int LeadId { get; set; }
        public string Reason { get; set; } = "COMPLETED";
    }

    public class DismissAdminEscalationDto
    {
        public int TaskId { get; set; }
        public int LeadId { get; set; }
        public string ResolutionNotes { get; set; } = string.Empty;
    }
}
