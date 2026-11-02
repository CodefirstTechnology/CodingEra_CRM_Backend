using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class PurchaseRequisition
    {
        public int Id { get; set; }

        public int TenantId { get; set; } = 1;

        public string PRNumber { get; set; } = string.Empty;

        public DateTime RequisitionDate { get; set; } = DateTime.UtcNow;

        public DateTime RequiredDate { get; set; } = DateTime.UtcNow.AddDays(14);

        public int DepartmentId { get; set; }

        public string Department { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public int CostCenterId { get; set; }

        public string CostCenterCode { get; set; } = string.Empty;

        public string Requestor { get; set; } = string.Empty;

        public string RequestedByUserId { get; set; } = string.Empty;

        public string Purpose { get; set; } = string.Empty;

        public PurchaseRequisitionPriority Priority { get; set; } = PurchaseRequisitionPriority.Normal;

        public PurchaseRequisitionStatus Status { get; set; } = PurchaseRequisitionStatus.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string InternalNotes { get; set; } = string.Empty;

        public decimal TotalEstimatedAmount { get; set; }

        public int CurrentApprovalTier { get; set; } = 1;

        public string ApprovalToken { get; set; } = string.Empty;

        public int? RFQId { get; set; }

        public string RFQNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public uint ConcurrencyVersion { get; set; }

        public List<PurchaseRequisitionLine> Lines { get; set; } = new();

        public List<PurchaseRequisitionStatusHistory> History { get; set; } = new();
    }
}
