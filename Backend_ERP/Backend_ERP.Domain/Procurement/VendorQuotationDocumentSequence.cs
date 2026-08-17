namespace ERP.Domain.Procurement
{
    public class VendorQuotationDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "VQ";

        public int LastSequence { get; set; }
    }
}
