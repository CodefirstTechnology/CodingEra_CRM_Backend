using System;

namespace ERP.Domain.Procurement
{
    public class RequestForQuotationStatusHistory
    {
        public int Id { get; set; }

        public int RequestForQuotationId { get; set; }

        public RequestForQuotation? RequestForQuotation { get; set; }

        public RFQStatus Status { get; set; }

        public RFQStatus? PreviousStatus { get; set; }

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
