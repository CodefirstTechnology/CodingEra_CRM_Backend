using System;

namespace ERP.Domain.Sales
{
    public class ProformaInvoiceApprovalHistory
    {
        public int Id { get; set; }

        public int ProformaInvoiceId { get; set; }

        public ProformaInvoice ProformaInvoice { get; set; } = null!;

        public string EntryKey { get; set; } = string.Empty;

        public string ApprovalLevel { get; set; } = string.Empty;

        public string Decision { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string ApprovedBy { get; set; } = string.Empty;

        public string? ApproverUserId { get; set; }

        public string? ApprovalToken { get; set; }

        public string? IpAddress { get; set; }

        public DateTimeOffset ApprovedOn { get; set; }
    }
}
