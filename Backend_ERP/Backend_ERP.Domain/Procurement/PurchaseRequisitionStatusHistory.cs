using System;

namespace ERP.Domain.Procurement
{
    public class PurchaseRequisitionStatusHistory
    {
        public int Id { get; set; }

        public int PurchaseRequisitionId { get; set; }

        public PurchaseRequisition? PurchaseRequisition { get; set; }

        public PurchaseRequisitionStatus Status { get; set; }

        public PurchaseRequisitionStatus? PreviousStatus { get; set; }

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
