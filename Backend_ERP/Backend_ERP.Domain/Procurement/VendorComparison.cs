using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class VendorComparison
    {
        public int Id { get; set; }

        public string ComparisonNumber { get; set; } = string.Empty;

        public int? RFQId { get; set; }

        public RequestForQuotation? RFQ { get; set; }

        public string RFQNumber { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public DateTime ComparisonDate { get; set; } = DateTime.UtcNow;

        public string RecommendationNotes { get; set; } = string.Empty;

        public int RecommendedVendorId { get; set; }

        public int? SelectedWinnerVendorId { get; set; }

        public string SelectedWinnerVendorName { get; set; } = string.Empty;

        public int? PurchaseOrderId { get; set; }

        public string PurchaseOrderNumber { get; set; } = string.Empty;

        public VendorComparisonStatus Status { get; set; } = VendorComparisonStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<VendorComparisonEntry> Entries { get; set; } = new();

        public List<VendorComparisonStatusHistory> History { get; set; } = new();
    }
}
