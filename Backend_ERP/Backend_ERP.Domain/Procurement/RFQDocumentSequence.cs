namespace ERP.Domain.Procurement
{
    public class RFQDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "RFQ";

        public int LastSequence { get; set; }
    }
}
