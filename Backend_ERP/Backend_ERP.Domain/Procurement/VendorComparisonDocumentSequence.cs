namespace ERP.Domain.Procurement
{
    public class VendorComparisonDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "VC";

        public int LastSequence { get; set; }
    }
}
