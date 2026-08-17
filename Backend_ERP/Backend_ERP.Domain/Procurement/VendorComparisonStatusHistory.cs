using System;

namespace ERP.Domain.Procurement
{
    public class VendorComparisonStatusHistory
    {
        public int Id { get; set; }

        public int VendorComparisonId { get; set; }

        public VendorComparison? VendorComparison { get; set; }

        public VendorComparisonStatus Status { get; set; }

        public VendorComparisonStatus? PreviousStatus { get; set; }

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
