using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class FinalInspection
    {
        public int Id { get; set; }

        public string InspectionNumber { get; set; } = string.Empty;

        public DateTime InspectionDate { get; set; } = DateTime.UtcNow;

        public int FinishedProductId { get; set; }

        public string FinishedProductCode { get; set; } = string.Empty;

        public string FinishedProductName { get; set; } = string.Empty;

        public int ProductionEntryId { get; set; }

        public string ProductionEntryNumber { get; set; } = string.Empty;

        public string ProductionBatch { get; set; } = string.Empty;

        public string Inspector { get; set; } = string.Empty;

        public string Dimension { get; set; } = string.Empty;

        public string Weight { get; set; } = string.Empty;

        public string Strength { get; set; } = string.Empty;

        public string SurfaceFinish { get; set; } = string.Empty;

        public string VisualCheck { get; set; } = string.Empty;

        public decimal AcceptedQuantity { get; set; }

        public decimal RejectedQuantity { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public FinalInspectionStatus Status { get; set; } = FinalInspectionStatus.Draft;

        public int? TestCertificateId { get; set; }

        public int? LoadTestId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<FinalInspectionParameter> Parameters { get; set; } = new();
    }
}
