using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class PurchaseRequisition
    {
        public int Id { get; set; }

        public string PRNumber { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string Requestor { get; set; } = string.Empty;

        public DateTime RequiredDate { get; set; } = DateTime.UtcNow.AddDays(14);

        public PurchaseRequisitionPriority Priority { get; set; } = PurchaseRequisitionPriority.Normal;

        public PurchaseRequisitionStatus Status { get; set; } = PurchaseRequisitionStatus.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string InternalNotes { get; set; } = string.Empty;

        public decimal TotalEstimatedAmount { get; set; }

        public int? RFQId { get; set; }

        public string RFQNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<PurchaseRequisitionLine> Lines { get; set; } = new();

        public List<PurchaseRequisitionStatusHistory> History { get; set; } = new();
    }
}
