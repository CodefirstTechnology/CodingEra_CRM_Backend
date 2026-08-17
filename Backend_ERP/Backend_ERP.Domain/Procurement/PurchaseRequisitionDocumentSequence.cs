namespace ERP.Domain.Procurement
{
    public class PurchaseRequisitionDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "PR";

        public int LastSequence { get; set; }
    }
}
