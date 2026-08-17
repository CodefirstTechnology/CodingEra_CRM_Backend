using System;

namespace ERP.Domain.Procurement
{
    public class PurchaseOrderStatusHistory
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        public PurchaseOrder? PurchaseOrder { get; set; }

        public PurchaseOrderStatus Status { get; set; }

        public PurchaseOrderStatus? PreviousStatus { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;
    }
}
