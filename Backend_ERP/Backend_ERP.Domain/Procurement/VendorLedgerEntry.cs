using System;

namespace ERP.Domain.Procurement
{
    public class VendorLedgerEntry
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public int? PurchaseBillId { get; set; }

        public PurchaseBill? PurchaseBill { get; set; }

        public string VoucherNumber { get; set; } = string.Empty;

        public DateTime EntryDate { get; set; } = DateTime.UtcNow;

        public string EntryType { get; set; } = "Bill"; // Bill, Payment, DebitNote, CreditNote

        public int? ReferenceId { get; set; }

        public string? ReferenceNumber { get; set; }

        public decimal DebitAmount { get; set; }

        public decimal CreditAmount { get; set; }

        public string? Narration { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }
    }
}
