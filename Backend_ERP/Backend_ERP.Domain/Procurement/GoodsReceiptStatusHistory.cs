using System;

namespace ERP.Domain.Procurement
{
    public class GoodsReceiptStatusHistory
    {
        public int Id { get; set; }

        public int GoodsReceiptId { get; set; }

        public GoodsReceipt? GoodsReceipt { get; set; }

        public GoodsReceiptStatus Status { get; set; }

        public GoodsReceiptStatus? PreviousStatus { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;
    }
}
