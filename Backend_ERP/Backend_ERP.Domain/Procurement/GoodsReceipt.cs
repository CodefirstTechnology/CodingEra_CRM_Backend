using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class GoodsReceipt
    {
        public int Id { get; set; }

        public string GRNNumber { get; set; } = string.Empty;

        public int PurchaseOrderId { get; set; }

        public PurchaseOrder? PurchaseOrder { get; set; }

        public string PurchaseOrderNumber { get; set; } = string.Empty;

        public string VendorName { get; set; } = string.Empty;

        public int? VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

        public int? WarehouseId { get; set; }

        public string Warehouse { get; set; } = "Main Store — Sanand";

        public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

        // Inward Gate Manifest & Logistics
        public string? GateInwardNumber { get; set; }

        public DateTime? GateInwardTime { get; set; }

        public string? VehicleNumber { get; set; }

        public string? TransporterName { get; set; }

        public string? LRNumber { get; set; }

        public string? EWayBillNumber { get; set; }

        // Weighbridge Weights
        public decimal? WeighbridgeGrossWeight { get; set; }

        public decimal? WeighbridgeTareWeight { get; set; }

        public decimal? WeighbridgeNetWeight { get; set; }

        // QC Quarantine & Valuation
        public string? InspectionLotNumber { get; set; }

        public QualityInspectionStatus QualityStatus { get; set; } = QualityInspectionStatus.Pending;

        public decimal TotalAcceptedAmount { get; set; }

        public decimal TotalLandedCost { get; set; }

        public bool IsPostedToInventory { get; set; }

        public DateTime? PostedToInventoryAt { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public uint RowVersion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<GoodsReceiptItem> Items { get; set; } = new();

        public List<GoodsReceiptStatusHistory> History { get; set; } = new();
    }
}
