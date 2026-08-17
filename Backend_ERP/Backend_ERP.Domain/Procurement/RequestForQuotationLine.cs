namespace ERP.Domain.Procurement
{
    public class RequestForQuotationLine
    {
        public int Id { get; set; }

        public int RequestForQuotationId { get; set; }

        public RequestForQuotation? RequestForQuotation { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "PCS";

        public decimal? TargetUnitPrice { get; set; }
    }
}
