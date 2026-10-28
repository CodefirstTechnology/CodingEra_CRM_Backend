using System;
using System.Collections.Generic;

namespace CRM.DTO
{
    public class SourceSummaryItemDto
    {
        public string SourceName { get; set; } = string.Empty;
        public int TotalLeads { get; set; }
        public int ContactedLeads { get; set; }
        public int QualifiedLeads { get; set; }
        public int QuotationsCount { get; set; }
        public int WonOrdersCount { get; set; }
        public int LostCount { get; set; }
        public int OpenCount { get; set; }
        public decimal TotalLeadValue { get; set; }
        public decimal TotalQuotedValue { get; set; }
        public decimal TotalWonRevenue { get; set; }
        public double ContactPercentage { get; set; }
        public double QualificationPercentage { get; set; }
        public double LeadToQuotePercentage { get; set; }
        public double QuoteToOrderPercentage { get; set; }
        public double ConversionRate { get; set; }
        public double ShareOfTotalLeadsPercentage { get; set; }
    }

    public class SourceMonthlyTrendDto
    {
        public string MonthLabel { get; set; } = string.Empty;
        public int MonthNumber { get; set; }
        public int Year { get; set; }
        public int TotalLeads { get; set; }
        public int QualifiedLeads { get; set; }
        public int QuotationsCount { get; set; }
        public int WonOrdersCount { get; set; }
        public decimal LeadValue { get; set; }
        public decimal QuotationValue { get; set; }
        public decimal WonRevenue { get; set; }
        public Dictionary<string, int> SourceLeadCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> SourceWonRevenues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class SourceSummaryTotalsDto
    {
        public int TotalLeads { get; set; }
        public int ContactedLeads { get; set; }
        public int QualifiedLeads { get; set; }
        public int QuotationsCount { get; set; }
        public int WonOrdersCount { get; set; }
        public int LostCount { get; set; }
        public int OpenCount { get; set; }
        public decimal TotalLeadValue { get; set; }
        public decimal TotalQuotedValue { get; set; }
        public decimal TotalWonRevenue { get; set; }
        public double AverageContactPercentage { get; set; }
        public double AverageQualificationPercentage { get; set; }
        public double AverageLeadToQuotePercentage { get; set; }
        public double AverageQuoteToOrderPercentage { get; set; }
        public double OverallConversionRate { get; set; }
    }

    public class SourceSummaryResponseDto
    {
        public int SelectedYear { get; set; }
        public string? SelectedSource { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string> ConfiguredSources { get; set; } = new();
        public List<SourceSummaryItemDto> Sources { get; set; } = new();
        public List<SourceMonthlyTrendDto> MonthlyTrends { get; set; } = new();
        public SourceSummaryTotalsDto Totals { get; set; } = new();
    }
}
