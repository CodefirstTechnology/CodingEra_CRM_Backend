namespace ERP.Domain.Procurement
{
    public class PurchaseOrderDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "PO";

        public int LastSequence { get; set; }
    }
}
