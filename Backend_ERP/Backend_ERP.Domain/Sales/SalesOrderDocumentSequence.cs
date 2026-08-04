namespace ERP.Domain.Sales
{
    /// <summary>Fiscal document sequence for sales order numbers (SO-YYYY-####).</summary>
    public class SalesOrderDocumentSequence
    {
        public int Id { get; set; }

        public int Year { get; set; }

        public int LastSequence { get; set; }
    }
}
