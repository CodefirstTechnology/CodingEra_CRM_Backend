namespace ERP.Domain.Sales
{
    public class Quotation
    {
        public int Id { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public int RevisionNumber { get; set; } = 1;

        public bool IsCurrentRevision { get; set; } = true;

        public string CustomerId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        public string? CustomerPhone { get; set; }

        public string BillingAddress { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string SalesPerson { get; set; } = string.Empty;

        public string Status { get; set; } = QuotationStatuses.Draft;

        public DateOnly QuotationDate { get; set; }

        public DateOnly ValidUntil { get; set; }

        public string Currency { get; set; } = "INR";

        public decimal ExchangeRate { get; set; } = 1.0m;

        public decimal Subtotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal FreightAmount { get; set; }

        public decimal PackagingAmount { get; set; }

        public decimal RoundOff { get; set; }

        public decimal GrandTotal { get; set; }

        public string PaymentTerms { get; set; } = string.Empty;

        public string DeliveryTerms { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public string? ClientPoNumber { get; set; }

        public DateTimeOffset? ClientAcceptedAt { get; set; }

        public string? ClientPoAttachmentUrl { get; set; }

        public int? ConvertedSalesOrderId { get; set; }

        public string? ConvertedSalesOrderNumber { get; set; }

        public DateTimeOffset? ConvertedOn { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();

        public void CalculateTotals()
        {
            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal taxTotal = 0;

            foreach (var item in Items)
            {
                subtotal += (item.Quantity * item.UnitPrice);
                discountTotal += item.DiscountAmount;
                taxTotal += item.TaxAmount;
            }

            Subtotal = subtotal;
            DiscountTotal = discountTotal;
            TaxTotal = taxTotal;
            GrandTotal = (Subtotal - DiscountTotal) + TaxTotal + FreightAmount + PackagingAmount + RoundOff;
        }
    }
}
