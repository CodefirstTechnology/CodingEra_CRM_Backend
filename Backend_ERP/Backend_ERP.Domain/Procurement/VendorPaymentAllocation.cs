using System;

namespace ERP.Domain.Procurement
{
    public class VendorPaymentAllocation
    {
        public int Id { get; set; }

        public int VendorPaymentId { get; set; }

        public VendorPayment? VendorPayment { get; set; }

        public int PurchaseBillId { get; set; }

        public PurchaseBill? PurchaseBill { get; set; }

        public decimal AllocatedAmount { get; set; }

        public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;
    }
}
