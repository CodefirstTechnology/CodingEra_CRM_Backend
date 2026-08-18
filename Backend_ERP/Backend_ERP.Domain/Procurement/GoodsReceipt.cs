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

        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

        public string Warehouse { get; set; } = "Main Store — Sanand";

        public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<GoodsReceiptItem> Items { get; set; } = new();

        public List<GoodsReceiptStatusHistory> History { get; set; } = new();
    }
}
