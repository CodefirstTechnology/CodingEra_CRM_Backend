using System;
using System.Collections.Generic;

namespace CRM.DTO
{
    public class LeadTrackerRowDto
    {
        public int LeadIdNum { get; set; }
        public string LeadId { get; set; } = string.Empty;
        public DateTime? LeadDate { get; set; }
        public string MonthFormatted { get; set; } = string.Empty;
        public string SalesExecutive { get; set; } = string.Empty;
        public int? SalesExecutiveUserId { get; set; }
        public string LeadSource { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string RequirementProduct { get; set; } = string.Empty;
        public decimal EstimatedLeadValue { get; set; }
        public DateTime? FirstContactDate { get; set; }
        public string Contacted { get; set; } = "No";
        public string Qualified { get; set; } = "No";
        public DateTime? MeetingDate { get; set; }
        public string MeetingDone { get; set; } = "No";
        public DateTime? QuotationDate { get; set; }
        public decimal QuotationValue { get; set; }
        public DateTime? LastFollowUpDate { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string FollowUpStatus { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Open, Won, Lost
        public int DaysOpen { get; set; }
        public string AgeingBucket { get; set; } = string.Empty;
        public DateTime? ExpectedOrderDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal OrderValue { get; set; }
        public string LostReason { get; set; } = "N/A";
    }

    public class LeadTrackerQueryParams
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? SortBy { get; set; } = "LeadIdNum";
        public string? SortDirection { get; set; } = "desc";
        public string? Search { get; set; }
        public string? SalesExecutive { get; set; }
        public string? Stage { get; set; }
        public string? Status { get; set; }
        public string? AgeingBucket { get; set; }
        public string? FollowUpStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class LeadTrackerSummaryDto
    {
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int WonLeads { get; set; }
        public int LostLeads { get; set; }
        public decimal TotalEstimatedValue { get; set; }
        public decimal TotalQuotationValue { get; set; }
        public decimal TotalWonValue { get; set; }
    }

    public class LeadTrackerPagedResultDto
    {
        public List<LeadTrackerRowDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 1));
        public LeadTrackerSummaryDto Summary { get; set; } = new();
    }
}
