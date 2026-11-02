using System;
using System.Collections.Generic;

namespace ERP.Application.Procurement.Dtos
{
    public class ConsolidationCandidateLineDto
    {
        public int Id { get; set; }
        public int PurchaseRequisitionId { get; set; }
        public string PRNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int? ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal SourcedQuantity { get; set; }
        public decimal RemainingQuantity => Math.Max(0, Quantity - SourcedQuantity);
        public string Uom { get; set; } = "PCS";
        public decimal EstimatedPrice { get; set; }
        public DateTime RequiredDate { get; set; }
        public int? TargetWarehouseId { get; set; }
        public bool IsAvlRestricted { get; set; }
    }

    public class ConsolidateLinesRequestDto
    {
        public List<int> PurchaseRequisitionLineIds { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public DateTime BidClosingDate { get; set; } = DateTime.UtcNow.AddDays(7);
        public bool IsSealedBid { get; set; } = true;
        public string SourcingType { get; set; } = "OpenCompetitive";
        public string DeliveryTerms { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = string.Empty;
        public string VendorNotes { get; set; } = string.Empty;
    }
}

