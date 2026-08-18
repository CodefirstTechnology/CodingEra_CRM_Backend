namespace ERP.Domain.Procurement
{
    public class QualityControlDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "INSP";

        public int LastSequence { get; set; }
    }
}
