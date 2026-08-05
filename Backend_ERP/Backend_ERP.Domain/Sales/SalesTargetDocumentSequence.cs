namespace ERP.Domain.Sales
{
    public class SalesTargetDocumentSequence
    {
        public int Id { get; set; }

        public int FinancialYear { get; set; }

        public string Prefix { get; set; } = "TGT";

        public int LastNumber { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
    }
}
