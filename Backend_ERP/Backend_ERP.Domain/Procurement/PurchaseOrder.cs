using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        public string PurchaseOrderNumber { get; set; } = string.Empty;

        public string ReferenceNumber { get; set; } = string.Empty;

        public int? VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public string VendorContact { get; set; } = string.Empty;

        public string BillingAddress { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string VendorEmail { get; set; } = string.Empty;

        public string VendorPhone { get; set; } = string.Empty;

        public string GstNumber { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string PaymentTerms { get; set; } = "Net 30";

        public string DeliveryTerms { get; set; } = "Door Delivery";

        public string BuyerName { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        public string Remarks { get; set; } = string.Empty;

        public PurchaseOrderPriority Priority { get; set; } = PurchaseOrderPriority.Normal;

        public string Currency { get; set; } = "INR";

        public PurchaseOrderSourceType SourceType { get; set; } = PurchaseOrderSourceType.Manual;

        public string SalesOrderNumber { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime? SubmittedDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<PurchaseOrderLine> Lines { get; set; } = new();

        public List<PurchaseOrderStatusHistory> History { get; set; } = new();

        public List<PurchaseOrderApprovalHistory> ApprovalHistory { get; set; } = new();
    }
}
