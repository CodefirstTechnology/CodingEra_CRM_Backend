namespace ERP.Domain.Procurement
{
    public class RFQItemRequisitionLine
    {
        public int Id { get; set; }

        public int TenantId { get; set; } = 1;

        public int RequestForQuotationLineId { get; set; }

        public int RFQLineId
        {
            get => RequestForQuotationLineId;
            set => RequestForQuotationLineId = value;
        }

        public RequestForQuotationLine? RequestForQuotationLine { get; set; }

        public RequestForQuotationLine? RFQLine
        {
            get => RequestForQuotationLine;
            set => RequestForQuotationLine = value;
        }

        public int PurchaseRequisitionLineId { get; set; }

        public PurchaseRequisitionLine? PurchaseRequisitionLine { get; set; }

        public decimal AllocatedQuantity { get; set; }
    }
}
