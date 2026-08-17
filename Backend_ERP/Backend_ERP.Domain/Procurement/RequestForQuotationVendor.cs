using System;

namespace ERP.Domain.Procurement
{
    public class RequestForQuotationVendor
    {
        public int Id { get; set; }

        public int RequestForQuotationId { get; set; }

        public RequestForQuotation? RequestForQuotation { get; set; }

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public string ContactEmail { get; set; } = string.Empty;

        public string ContactPhone { get; set; } = string.Empty;

        public RFQVendorStatus Status { get; set; } = RFQVendorStatus.Pending;

        public DateTime? RespondedDate { get; set; }
    }
}
