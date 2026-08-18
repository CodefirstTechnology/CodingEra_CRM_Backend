using System;

namespace ERP.Domain.Procurement
{
    public class PurchaseBillHistory
    {
        public int Id { get; set; }

        public int PurchaseBillId { get; set; }

        public PurchaseBill? PurchaseBill { get; set; }

        public PurchaseBillStatus Status { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;
    }
}
