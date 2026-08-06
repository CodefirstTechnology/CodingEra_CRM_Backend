namespace ERP.Domain.Sales
{
    public class QuotationApprovalDocumentSequence
    {
        public int Id { get; set; }

        public int FinancialYear { get; set; }

        public string Prefix { get; set; } = string.Empty;

        public int LastNumber { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
    }
}
