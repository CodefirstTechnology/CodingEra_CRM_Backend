namespace ERP.Domain.Production
{
    public class ProductionDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = string.Empty;

        public int LastSequence { get; set; }
    }
}
