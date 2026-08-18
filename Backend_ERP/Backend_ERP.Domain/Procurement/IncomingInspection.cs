using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class IncomingInspection
    {
        public int Id { get; set; }

        public string InspectionNumber { get; set; } = string.Empty;

        public DateTime InspectionDate { get; set; } = DateTime.UtcNow;

        public int SupplierId { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public int VendorId { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public int PurchaseOrderId { get; set; }

        public string PurchaseOrderNumber { get; set; } = string.Empty;

        public int GRNId { get; set; }

        public string GRNNumber { get; set; } = string.Empty;

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public string BatchNumber { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public string Inspector { get; set; } = string.Empty;

        public InspectionType InspectionType { get; set; } = InspectionType.Visual;

        public InspectionMethod InspectionMethod { get; set; } = InspectionMethod.Sampling;

        public decimal SamplingQuantity { get; set; }

        public decimal AcceptedQuantity { get; set; }

        public decimal RejectedQuantity { get; set; }

        public decimal PendingQuantity { get; set; }

        public InspectionResult InspectionResult { get; set; } = InspectionResult.Pending;

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public IncomingInspectionStatus Status { get; set; } = IncomingInspectionStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<IncomingChecklistItem> Checklist { get; set; } = new();
    }
}
