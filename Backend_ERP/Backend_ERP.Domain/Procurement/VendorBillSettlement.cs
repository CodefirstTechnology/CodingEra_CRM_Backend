using System;

namespace ERP.Domain.Procurement
{
    public class VendorBillSettlement
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "TENANT-DEFAULT";

        public int PurchaseBillId { get; set; }

        public PurchaseBill? PurchaseBill { get; set; }

        public int? PaymentVoucherId { get; set; }

        public VendorPayment? PaymentVoucher { get; set; }

        public int? DebitNoteId { get; set; } // Link to Return-to-Vendor Debit Note

        public decimal AllocatedAmount { get; set; }

        public DateTime SettlementDate { get; set; } = DateTime.UtcNow;

        public decimal RealizedFxGainLoss { get; set; } // Positive = FX Gain, Negative = FX Loss

        public string? AllocationNotes { get; set; }

        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

