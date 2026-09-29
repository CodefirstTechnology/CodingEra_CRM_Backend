using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class VendorPayment
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string PaymentNumber { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public string PaymentMethod { get; set; } = "NEFT/RTGS";

        public int? BankAccountId { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal UnallocatedAmount { get; set; }

        public string? ReferenceNumber { get; set; }

        public string Status { get; set; } = "Cleared";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }

        public List<VendorPaymentAllocation> Allocations { get; set; } = new();
    }
}
