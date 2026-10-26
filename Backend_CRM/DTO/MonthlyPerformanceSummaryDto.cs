using System;
using System.Collections.Generic;

namespace CRM.DTO
{
    public class MonthlySummaryRowDto
    {
        public string MonthLabel { get; set; } = string.Empty;
        public int MonthNumber { get; set; }
        public int TotalLeads { get; set; }
        public int QualifiedLeads { get; set; }
        public int QuotationsCount { get; set; }
        public int OrdersWonCount { get; set; }
        public decimal LeadValue { get; set; }
        public decimal QuotationValue { get; set; }
        public decimal OrderValue { get; set; }
        public double LeadToQuotePercentage { get; set; }
        public double QuoteToOrderPercentage { get; set; }
        public double LeadToOrderPercentage { get; set; }
    }

    public class MonthlySummaryTotalsDto
    {
        public int TotalLeads { get; set; }
        public int QualifiedLeads { get; set; }
        public int QuotationsCount { get; set; }
        public int OrdersWonCount { get; set; }
        public decimal LeadValue { get; set; }
        public decimal QuotationValue { get; set; }
        public decimal OrderValue { get; set; }
        public double LeadToQuotePercentage { get; set; }
        public double QuoteToOrderPercentage { get; set; }
        public double LeadToOrderPercentage { get; set; }
    }

    public class MonthlyPerformanceReportResponse
    {
        public int SelectedYear { get; set; }
        public List<MonthlySummaryRowDto> Months { get; set; } = new();
        public MonthlySummaryTotalsDto Totals { get; set; } = new();
    }
}
