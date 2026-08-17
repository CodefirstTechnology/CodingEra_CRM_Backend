using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ERP.Application.Procurement.Dtos
{
    public class VendorQuotationLineDto
    {
        public int Id { get; set; }

        public int VendorQuotationId { get; set; }

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("uom")]
        public string Uom { get; set; } = "PCS";

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("taxPercent")]
        public decimal TaxPercent { get; set; }

        [JsonPropertyName("discountPercent")]
        public decimal DiscountPercent { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }

    public class VendorQuotationDto
    {
        public int Id { get; set; }

        [JsonPropertyName("quotationNumber")]
        public string QuotationNumber { get; set; } = string.Empty;

        [JsonPropertyName("rfqId")]
        public int? RequestForQuotationId { get; set; }

        [JsonPropertyName("vendorId")]
        public int VendorId { get; set; }

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("quotationRef")]
        public string QuotationRef { get; set; } = string.Empty;

        [JsonPropertyName("quotationDate")]
        public DateTime QuotationDate { get; set; }

        [JsonPropertyName("validityDate")]
        public DateTime? ValidityDate { get; set; }

        [JsonPropertyName("deliveryTimeDays")]
        public int DeliveryTimeDays { get; set; }

        [JsonPropertyName("leadTime")]
        public string LeadTime { get; set; } = string.Empty;

        [JsonPropertyName("warrantyPeriod")]
        public string WarrantyPeriod { get; set; } = string.Empty;

        [JsonPropertyName("paymentTerms")]
        public string PaymentTerms { get; set; } = string.Empty;

        [JsonPropertyName("subTotal")]
        public decimal SubTotal { get; set; }

        [JsonPropertyName("discountPercent")]
        public decimal DiscountPercent { get; set; }

        [JsonPropertyName("discountAmount")]
        public decimal DiscountAmount { get; set; }

        [JsonPropertyName("taxPercent")]
        public decimal TaxPercent { get; set; }

        [JsonPropertyName("taxAmount")]
        public decimal TaxAmount { get; set; }

        [JsonPropertyName("totalCost")]
        public decimal TotalCost { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public List<VendorQuotationLineDto> Lines { get; set; } = new();
    }

    public class VendorQuotationCreateRequestDto
    {
        public int? RequestForQuotationId { get; set; }

        public int VendorId { get; set; }

        public string QuotationRef { get; set; } = string.Empty;

        public DateTime QuotationDate { get; set; } = DateTime.UtcNow;

        public DateTime? ValidityDate { get; set; }

        public int DeliveryTimeDays { get; set; }

        public string LeadTime { get; set; } = string.Empty;

        public string WarrantyPeriod { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;

        public decimal DiscountPercent { get; set; }

        public decimal TaxPercent { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public List<VendorQuotationLineDto> Lines { get; set; } = new();
    }
}
