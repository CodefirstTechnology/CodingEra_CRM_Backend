namespace ERP.Domain.Sales
{
    public class SalesOrderStatusHistory
    {
        public int Id { get; set; }

        public int SalesOrderId { get; set; }

        public SalesOrder SalesOrder { get; set; } = null!;

        /// <summary>Frontend wire id (GUID string).</summary>
        public string EntryKey { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset Date { get; set; }

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string? Label { get; set; }
    }
}
