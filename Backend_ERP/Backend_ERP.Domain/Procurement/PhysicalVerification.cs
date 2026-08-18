using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class PhysicalVerification
    {
        public int Id { get; set; }

        public string VerificationNumber { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public string Verifier { get; set; } = string.Empty;

        public DateTime VerificationDate { get; set; } = DateTime.UtcNow;

        public decimal ExpectedQuantity { get; set; }

        public decimal ActualQuantity { get; set; }

        public decimal Variance { get; set; }

        public decimal VarianceValue { get; set; }

        public VerificationStatus Status { get; set; } = VerificationStatus.Scheduled;

        public bool AdjustmentPosted { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public string? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<PhysicalVerificationLine> Lines { get; set; } = new();
    }
}
