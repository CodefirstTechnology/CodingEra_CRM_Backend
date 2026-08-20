namespace ERP.Domain.Sales
{
    public class DispatchDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "DN";

        public int LastSequence { get; set; }
    }
}
