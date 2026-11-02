using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class RequestForQuotationLine
    {
        public int Id { get; set; }

        public int TenantId { get; set; } = 1;

        public int RequestForQuotationId { get; set; }

        public RequestForQuotation? RequestForQuotation { get; set; }

        public int? ItemId { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "PCS";

        public decimal? TargetUnitPrice { get; set; }

        public string TechnicalSpecsJson { get; set; } = "{}";

        public List<RFQItemRequisitionLine> ConsolidatedRequisitionLines { get; set; } = new();
    }
}
