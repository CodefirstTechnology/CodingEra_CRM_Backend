namespace ERP.Domain.Sales
{
    public class ProformaInvoiceStatusHistory
    {
        public int Id { get; set; }

        public int ProformaInvoiceId { get; set; }

        public ProformaInvoice ProformaInvoice { get; set; } = null!;

        public string EntryKey { get; set; } = string.Empty;

        public string? OldStatus { get; set; }

        public string NewStatus { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string ChangedBy { get; set; } = string.Empty;

        public DateTimeOffset ChangedOn { get; set; }
    }
}
