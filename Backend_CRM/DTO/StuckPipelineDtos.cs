using System;
using System.Collections.Generic;

namespace CRM.DTO
{
    public class StuckPipelineSummaryDto
    {
        public decimal StuckValue { get; set; }
        public int StuckDealsCount { get; set; }
        public double AvgIdleHours { get; set; }
        public string AvgIdleTimeFormatted { get; set; } = string.Empty;
        public int IdleLeadsCount { get; set; }
    }

    public class StuckDealItemDto
    {
        public int DealId { get; set; }
        public string DealTitle { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public decimal DealAmount { get; set; }
        public DateTime LastActivityAt { get; set; }
        public int IdleHours { get; set; }
        public string IdleDurationFormatted { get; set; } = string.Empty;
        public int? OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
    }

    public class IdleLeadItemDto
    {
        public int LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string Status { get; set; } = "New";
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public int IdleHours { get; set; }
        public string IdleDurationFormatted { get; set; } = string.Empty;
        public int? OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class StuckPipelineResponseDto
    {
        public StuckPipelineSummaryDto Summary { get; set; } = new();
        public List<StuckDealItemDto> StuckDeals { get; set; } = new();
        public List<IdleLeadItemDto> IdleLeads { get; set; } = new();
    }
}
