using System;
using System.Collections.Generic;

namespace CRM.DTO
{
    public class SalesExecutiveReportQueryDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TimeZoneOffsetMinutes { get; set; } = -330; // IST UTC+5:30 default
        public int? RoleId { get; set; }
        public bool IncludeInactiveUsers { get; set; } = false;
    }

    public class SalesExecutiveReportRowDto
    {
        public int UserId { get; set; }
        public string ExecutiveName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        // Period-Scoped Counts (Cols 2-7)
        public int TotalLeads { get; set; }
        public int Contacted { get; set; }
        public int Qualified { get; set; }
        public int Meetings { get; set; }
        public int Quotations { get; set; }
        public int OrdersWon { get; set; }

        // Period-Scoped Financial Values (Cols 8-10)
        public decimal LeadValue { get; set; }
        public decimal QuotationValue { get; set; }
        public decimal OrderValue { get; set; }

        // Period-Scoped Ratios & Compliance (Cols 11-15)
        public double ContactPercentage { get; set; }
        public double QualificationPercentage { get; set; }
        public double QuotePercentage { get; set; }
        public double OrderConversionPercentage { get; set; }
        public double FollowUpCompliancePercentage { get; set; }

        // Point-in-Time Active Snapshots (Cols 16-17)
        public int OpenLeads { get; set; }
        public int OverdueFollowUps { get; set; }
    }

    public class SalesExecutiveReportSummaryDto
    {
        public int TotalExecutives { get; set; }
        public int TotalLeads { get; set; }
        public int TotalContacted { get; set; }
        public int TotalQualified { get; set; }
        public int TotalMeetings { get; set; }
        public int TotalQuotations { get; set; }
        public int TotalOrdersWon { get; set; }
        public decimal TotalLeadValue { get; set; }
        public decimal TotalQuotationValue { get; set; }
        public decimal TotalOrderValue { get; set; }

        // Ratios calculated from Sum of Numerators / Sum of Denominators
        public double AverageContactPercentage { get; set; }
        public double AverageQualificationPercentage { get; set; }
        public double AverageQuotePercentage { get; set; }
        public double AverageOrderConversionPercentage { get; set; }
        public double AverageFollowUpCompliancePercentage { get; set; }

        public int TotalOpenLeads { get; set; }
        public int TotalOverdueFollowUps { get; set; }
    }

    public class SalesExecutiveReportResponseDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SalesExecutiveReportSummaryDto Summary { get; set; } = new();
        public List<SalesExecutiveReportRowDto> Rows { get; set; } = new();
    }
}
