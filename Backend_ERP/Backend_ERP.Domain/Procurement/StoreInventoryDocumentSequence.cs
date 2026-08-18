namespace ERP.Domain.Procurement
{
    public class StoreInventoryDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "WH";

        public int LastSequence { get; set; }
    }
}
