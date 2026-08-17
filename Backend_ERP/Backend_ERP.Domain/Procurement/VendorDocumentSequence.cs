namespace ERP.Domain.Procurement
{
    public class VendorDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "VEN";

        public int LastSequence { get; set; }
    }
}
