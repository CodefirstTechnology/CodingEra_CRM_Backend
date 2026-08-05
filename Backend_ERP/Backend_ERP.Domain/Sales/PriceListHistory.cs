namespace ERP.Domain.Sales
{
    public class PriceListHistory
    {
        public int Id { get; set; }

        public int PriceListId { get; set; }

        public PriceList PriceList { get; set; } = null!;

        public string Action { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string ChangedBy { get; set; } = string.Empty;

        public DateTimeOffset ChangedOn { get; set; }
    }
}
