namespace ERP.Domain.Procurement
{
    public class PurchaseBillDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "BILL";

        public int LastSequence { get; set; }
    }
}
