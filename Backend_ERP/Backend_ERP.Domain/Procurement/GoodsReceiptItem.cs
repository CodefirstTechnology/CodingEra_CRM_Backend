using System;

namespace ERP.Domain.Procurement
{
    public class GoodsReceiptItem
    {
        public int Id { get; set; }

        public int GoodsReceiptId { get; set; }

        public GoodsReceipt? GoodsReceipt { get; set; }

        public int PurchaseOrderLineId { get; set; }

        public PurchaseOrderLine? PurchaseOrderLine { get; set; }

        public int? ItemId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Unit { get; set; } = "Nos";

        public decimal OrderedQuantity { get; set; }

        public decimal PreviouslyReceivedQuantity { get; set; }

        public decimal RemainingQuantity { get; set; }

        public decimal ReceivedQuantity { get; set; }

        public decimal AcceptedQuantity { get; set; }

        public decimal RejectedQuantity { get; set; }

        public string RejectionReasonCode { get; set; } = string.Empty;

        // Industrial Batch & Traceability
        public string? BatchNumber { get; set; }

        public string? HeatNumber { get; set; }

        public string? MtcNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }

        // Valuation & Rates
        public decimal PoRate { get; set; }

        public decimal LandedCostPerUnit { get; set; }

        public decimal ValuationAmount { get; set; }

        // QC Disposition Buckets
        public decimal QcInspectedQuantity { get; set; }

        public decimal QcAcceptedQuantity { get; set; }

        public decimal QcRejectedQuantity { get; set; }

        public decimal QcReworkQuantity { get; set; }

        public string? QcDispositionNotes { get; set; }

        // Cumulative Billed Quantity
        public decimal BilledQuantity { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}
