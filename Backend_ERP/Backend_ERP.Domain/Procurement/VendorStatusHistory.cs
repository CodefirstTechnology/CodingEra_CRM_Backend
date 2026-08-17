using System;

namespace ERP.Domain.Procurement
{
    public class VendorStatusHistory
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public VendorStatus Status { get; set; }

        public VendorStatus? PreviousStatus { get; set; }

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
