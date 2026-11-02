using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Procurement
{
    public class PurchaseOrderLine
    {
        public int Id { get; set; }
        public int TenantId { get; set; } = 1;

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        public int LineNumber { get; set; } = 1;

        // Item Identification
        public POItemType ItemType { get; set; } = POItemType.Inventory;
        public int? ItemId { get; set; }
        public int? UomId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HsnCode { get; set; } = string.Empty;
        public string Unit { get; set; } = "Nos";
        public string BaseUom { get => Unit; set => Unit = value; }

        // Quantities & Tracking (precision 18,4)
        public decimal Quantity { get; set; }
        public decimal OrderedQuantity { get => Quantity; set => Quantity = value; }
        public decimal ReceivedQuantity { get; set; } = 0.0000m;
        public decimal RejectedQuantity { get; set; } = 0.0000m;
        public decimal InvoicedQuantity { get; set; } = 0.0000m;
        public decimal BilledQuantity { get => InvoicedQuantity; set => InvoicedQuantity = value; }
        public decimal CancelledQuantity { get; set; } = 0.0000m;
        public decimal OverDeliveryTolerancePercent { get; set; } = 0.00m;

        // Rates & Financials
        public decimal Rate { get; set; }
        public decimal UnitRate { get => Rate; set => Rate = value; }
        public decimal Discount { get; set; } = 0.00m; // Discount percentage
        public decimal DiscountPercent { get => Discount; set => Discount = value; }
        public decimal DiscountAmount { get; set; } = 0.00m;
        public decimal TaxableAmount { get; set; } = 0.00m;
        public decimal GstRatePercentage { get; set; } = 18.00m;
        [NotMapped]
        public decimal TaxPercentage { get => GstRatePercentage; set => GstRatePercentage = value; }
        public decimal CgstAmount { get; set; } = 0.00m;
        public decimal SgstAmount { get; set; } = 0.00m;
        public decimal IgstAmount { get; set; } = 0.00m;
        public decimal Tax { get; set; } = 0.00m;
        public decimal TaxAmount { get => Tax; set => Tax = value; }
        public decimal Amount { get; set; } = 0.00m;
        public decimal LineTotal { get => Amount; set => Amount = value; }
        public bool IsItcEligible { get; set; } = true;

        // Logistics & Scheduling
        public int? TargetWarehouseId { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string DeliveryScheduleJson { get; set; } = "[]";
        public PurchaseOrderLineStatus LineStatus { get; set; } = PurchaseOrderLineStatus.PendingReceipt;

        // Upstream Lineage Bridges
        public int? PurchaseRequisitionLineId { get; set; }
        public PurchaseRequisitionLine? PurchaseRequisitionLine { get; set; }
        public int? RFQLineId { get; set; }
        public RequestForQuotationLine? RFQLine { get; set; }
        public int? VendorQuotationLineId { get; set; }
        public VendorQuotationLine? VendorQuotationLine { get; set; }

        public uint ConcurrencyVersion { get; set; }
        public uint Version { get => ConcurrencyVersion; set => ConcurrencyVersion = value; }
    }
}
