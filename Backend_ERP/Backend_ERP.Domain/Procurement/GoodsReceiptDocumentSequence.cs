namespace ERP.Domain.Procurement
{
    public class GoodsReceiptDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "GRN";

        public int LastSequence { get; set; }
    }
}
