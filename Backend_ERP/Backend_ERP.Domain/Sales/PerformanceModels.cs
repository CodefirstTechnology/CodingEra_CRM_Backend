namespace ERP.Domain.Sales
{
    public static class PerformancePeriodTypes
    {
        public const string Daily = "Daily";
        public const string Weekly = "Weekly";
        public const string Monthly = "Monthly";
        public const string Quarterly = "Quarterly";
        public const string HalfYearly = "HalfYearly";
        public const string Yearly = "Yearly";
        public const string Custom = "Custom";

        public static readonly string[] All =
        [
            Daily, Weekly, Monthly, Quarterly, HalfYearly, Yearly, Custom
        ];
    }

    public class PerformanceSnapshot
    {
        public int Id { get; set; }
        public DateOnly SnapshotDate { get; set; }
        public string PeriodType { get; set; } = PerformancePeriodTypes.Monthly;
        public int? SalesPersonUserId { get; set; }
        public string SalesTeam { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string RegionalManager { get; set; } = string.Empty;
        public int TotalSalesOrders { get; set; }
        public int ConfirmedSalesOrders { get; set; }
        public int CompletedSalesOrders { get; set; }
        public int CancelledSalesOrders { get; set; }
        public decimal SalesOrderAmount { get; set; }
        public int ProformaInvoiceCount { get; set; }
        public int ApprovedProformaInvoices { get; set; }
        public int ConvertedProformaInvoices { get; set; }
        public decimal ProformaAmount { get; set; }
        public decimal AdvancePaymentReceived { get; set; }
        public decimal AdvancePaymentApplied { get; set; }
        public decimal OutstandingAdvance { get; set; }
        public decimal AssignedTarget { get; set; }
        public decimal AchievedTarget { get; set; }
        public decimal AchievementPercentage { get; set; }
        public decimal CollectionPercentage { get; set; }
        public decimal ConversionPercentage { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }

    public class PerformanceHistory
    {
        public int Id { get; set; }
        public int? SalesPersonUserId { get; set; }
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public decimal OldValue { get; set; }
        public decimal NewValue { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public DateTimeOffset RecordedOn { get; set; }
    }

    public class PerformanceExportHistory
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTimeOffset GeneratedOn { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ExportType { get; set; } = string.Empty;
    }
}
