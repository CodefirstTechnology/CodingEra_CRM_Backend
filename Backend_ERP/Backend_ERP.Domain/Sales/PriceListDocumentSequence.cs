namespace ERP.Domain.Sales
{
    public class PriceListDocumentSequence
    {
        public int Id { get; set; }

        public int FinancialYear { get; set; }

        public string Prefix { get; set; } = "PL";

        public int LastNumber { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
    }
}
