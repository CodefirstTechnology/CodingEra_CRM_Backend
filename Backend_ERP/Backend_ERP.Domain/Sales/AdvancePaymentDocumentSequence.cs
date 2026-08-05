namespace ERP.Domain.Sales
{
    public class AdvancePaymentDocumentSequence
    {
        public int Id { get; set; }

        public int FinancialYear { get; set; }

        public string Prefix { get; set; } = "ADV";

        public int LastNumber { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
    }
}
