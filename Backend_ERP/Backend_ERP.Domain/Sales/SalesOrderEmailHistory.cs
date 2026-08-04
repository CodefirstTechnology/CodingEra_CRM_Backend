namespace ERP.Domain.Sales
{
    public class SalesOrderEmailHistory
    {
        public int Id { get; set; }

        public int SalesOrderId { get; set; }

        public SalesOrder SalesOrder { get; set; } = null!;

        public string EntryKey { get; set; } = string.Empty;

        public string Recipient { get; set; } = string.Empty;

        public DateTimeOffset SentDate { get; set; }

        public string Action { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
