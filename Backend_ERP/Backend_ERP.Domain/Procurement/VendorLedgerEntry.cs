using System;

namespace ERP.Domain.Procurement
{
    public class VendorLedgerEntry
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "TENANT-DEFAULT";

        public string EntryNumber { get; set; } = string.Empty; // e.g. VLE-2026-000001

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public int? PurchaseBillId { get; set; }

        public PurchaseBill? PurchaseBill { get; set; }

        public string VoucherNumber { get; set; } = string.Empty;

        public DateTime PostingDate { get; set; } = DateTime.UtcNow;

        // Backward compatibility alias for EntryDate
        public DateTime EntryDate
        {
            get => PostingDate;
            set => PostingDate = value;
        }

        public string VoucherType { get; set; } = "Bill"; // Bill, Payment, DebitNote, Advance, FxRevaluation

        // Backward compatibility alias for EntryType
        public string EntryType
        {
            get => VoucherType;
            set => VoucherType = value;
        }

        public string? ReferenceDocType { get; set; } // PurchaseOrder, GoodsReceipt, PurchaseBill

        public int? ReferenceDocId { get; set; }

        // Backward compatibility alias for ReferenceId
        public int? ReferenceId
        {
            get => ReferenceDocId;
            set => ReferenceDocId = value;
        }

        public string? ReferenceDocNumber { get; set; }

        // Backward compatibility alias for ReferenceNumber
        public string? ReferenceNumber
        {
            get => ReferenceDocNumber;
            set => ReferenceDocNumber = value;
        }

        // Multi-Currency & FX Valuation
        public string Currency { get; set; } = "INR";

        public decimal ExchangeRate { get; set; } = 1.000000m;

        public decimal ForeignDebitAmount { get; set; }

        public decimal ForeignCreditAmount { get; set; }

        // Base Functional Currency (INR) Amounts
        public decimal DebitAmount { get; set; }

        public decimal CreditAmount { get; set; }

        public decimal RunningBalance { get; set; }

        // Settlement Tracking (Knock-off)
        public decimal SettledAmount { get; set; }

        public decimal OutstandingAmount { get; set; }

        public string SettlementStatus { get; set; } = "Unsettled"; // Unsettled, PartiallySettled, FullySettled

        public string? Narration { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedByUserId { get; set; }

        // Concurrency token (PostgreSQL xmin)
        public uint RowVersion { get; set; }
    }
}
