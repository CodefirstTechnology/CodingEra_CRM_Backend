namespace ERP.Domain.Sales
{
    public class PriceList
    {
        public int Id { get; set; }

        public string PriceListNumber { get; set; } = string.Empty;

        public string PriceListName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CustomerCategory { get; set; } = PriceListCustomerCategories.Standard;

        public string Currency { get; set; } = "INR";

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public string Status { get; set; } = PriceListStatuses.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();

        public ICollection<PriceListHistory> History { get; set; } = new List<PriceListHistory>();
    }
}
