namespace ERP.Domain.Procurement
{
    public class PurchaseRequisitionLine
    {
        public int Id { get; set; }

        public int PurchaseRequisitionId { get; set; }

        public PurchaseRequisition? PurchaseRequisition { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "PCS";

        public decimal EstimatedPrice { get; set; }

        public decimal TotalAmount { get; set; }
    }
}
