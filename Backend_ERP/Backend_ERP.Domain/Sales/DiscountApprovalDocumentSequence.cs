namespace ERP.Domain.Sales
{
    public class DiscountApprovalDocumentSequence
    {
        public int Id { get; set; }

        public int FinancialYear { get; set; }

        public string Prefix { get; set; } = "DA";

        public int LastNumber { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
    }
}
