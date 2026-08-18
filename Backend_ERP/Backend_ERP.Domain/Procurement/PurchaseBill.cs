using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class PurchaseBill
    {
        public int Id { get; set; }

        public string BillNumber { get; set; } = string.Empty;

        public string InvoiceNumber { get; set; } = string.Empty;

        public int VendorId { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public int? PurchaseOrderId { get; set; }

        public string? PurchaseOrderNumber { get; set; }

        public int? GRNId { get; set; }

        public string? GRNNumber { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public string Currency { get; set; } = "INR";

        public decimal SubTotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal RoundOff { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal BalanceAmount { get; set; }

        public string PaymentTerms { get; set; } = "Net 30 Days";

        public PurchaseBillPaymentStatus PaymentStatus { get; set; } = PurchaseBillPaymentStatus.Unpaid;

        public PurchaseBillStatus Status { get; set; } = PurchaseBillStatus.Draft;

        public string Remarks { get; set; } = string.Empty;

        public int AttachmentsCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<PurchaseBillLine> Lines { get; set; } = new();

        public List<PurchaseBillHistory> History { get; set; } = new();
    }
}
