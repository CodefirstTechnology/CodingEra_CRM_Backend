namespace ERP.Domain.Procurement
{
    public class VendorQuotationLine
    {
        public int Id { get; set; }

        public int TenantId { get; set; } = 1;

        public int VendorQuotationId { get; set; }

        public VendorQuotation? VendorQuotation { get; set; }

        public int? ItemId { get; set; }

        public int? UomId { get; set; }

        public int? RequestForQuotationLineId { get; set; }

        public RequestForQuotationLine? RequestForQuotationLine { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "PCS";

        public decimal UnitPrice { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal Amount { get; set; }

        public decimal PackagingAmount { get; set; } = 0m;

        public decimal FreightAmount { get; set; } = 0m;

        public decimal InsuranceAmount { get; set; } = 0m;

        public string HsnCode { get; set; } = string.Empty;

        public decimal GstRatePercentage { get; set; } = 18m;

        public bool IsItcEligible { get; set; } = true;

        public decimal NetLandedCostPerUnit { get; set; } = 0m;

        public int PromisedLeadTimeDays { get; set; } = 14;
    }
}
