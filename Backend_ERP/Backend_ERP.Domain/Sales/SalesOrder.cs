using System;
using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public class SalesOrder
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public string SalesOrderNumber { get; set; } = string.Empty;

        public int? QuotationId { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public string? ApprovalToken { get; set; }

        public uint Version { get; set; }

        public string SourceType { get; set; } = SalesOrderSourceTypes.Manual;

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerCategory { get; set; } = "Standard";

        public string ContactPerson { get; set; } = string.Empty;

        public string BillingAddress { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string PlaceOfSupplyStateCode { get; set; } = "27";

        public string? CustomerEmail { get; set; }

        public string? CustomerPhone { get; set; }

        public string SalesPerson { get; set; } = string.Empty;

        public int? SalesPersonUserId { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateOnly OrderDate { get; set; }

        public DateOnly? ExpectedDeliveryDate { get; set; }

        public string PaymentTerms { get; set; } = string.Empty;

        public string DeliveryTerms { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal GstTotal { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal AdvanceAllocatedAmount { get; set; } = 0m;

        public decimal ProformaInvoicedAmount { get; set; } = 0m;

        public decimal FinalInvoicedAmount { get; set; } = 0m;

        public decimal TotalAmount => GrandTotal;

        // Two-Tier Release Gates
        public bool IsProductionReleased { get; set; } = false;

        public DateTimeOffset? ProductionReleasedDate { get; set; }

        public string? ProductionReleasedBy { get; set; }

        public bool IsDispatchReleased { get; set; } = false;

        public DateTimeOffset? DispatchReleasedDate { get; set; }

        public string? DispatchReleasedBy { get; set; }

        // Lifecycle & Credit Management
        public string Status { get; set; } = SalesOrderStatuses.Draft;

        public string? CreditHoldReason { get; set; }

        public DateTimeOffset? CreditApprovedDate { get; set; }

        public string? CreditApprovedBy { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public DateTimeOffset? PdfGeneratedDate { get; set; }

        public DateTimeOffset? LastCommunicationDate { get; set; }

        public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();

        public ICollection<SalesOrderAllocation> Allocations { get; set; } = new List<SalesOrderAllocation>();

        public ICollection<SalesOrderStatusHistory> StatusHistory { get; set; } = new List<SalesOrderStatusHistory>();

        public ICollection<SalesOrderEmailHistory> EmailHistory { get; set; } = new List<SalesOrderEmailHistory>();
    }
}
