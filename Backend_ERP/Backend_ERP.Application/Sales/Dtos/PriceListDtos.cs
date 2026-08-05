namespace ERP.Application.Sales.Dtos
{
    public class PriceListItemDto
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemCategory { get; set; } = string.Empty;
        public string Unit { get; set; } = "Nos";
        public decimal BasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumDiscount { get; set; }
        public decimal TaxPercentage { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class PriceListHistoryDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public string ChangedOn { get; set; } = string.Empty;
    }

    public class PriceListDto
    {
        public int Id { get; set; }
        public string PriceListNumber { get; set; } = string.Empty;
        public string PriceListName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public string EffectiveFrom { get; set; } = string.Empty;
        public string? EffectiveTo { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedDate { get; set; } = string.Empty;
        public List<PriceListItemDto> Items { get; set; } = new();
        public List<PriceListHistoryDto> History { get; set; } = new();
    }

    public class PriceListListItemDto
    {
        public int Id { get; set; }
        public string PriceListNumber { get; set; } = string.Empty;
        public string PriceListName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string EffectiveFrom { get; set; } = string.Empty;
        public string? EffectiveTo { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class PriceListItemRequestDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemCategory { get; set; } = string.Empty;
        public string Unit { get; set; } = "Nos";
        public decimal BasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumDiscount { get; set; }
        public decimal TaxPercentage { get; set; }
        public string? Remarks { get; set; }
        public int? SortOrder { get; set; }
    }

    public class PriceListCreateRequestDto
    {
        public string? PriceListNumber { get; set; }
        public string PriceListName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CustomerCategory { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public string EffectiveFrom { get; set; } = string.Empty;
        public string? EffectiveTo { get; set; }
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public List<PriceListItemRequestDto> Items { get; set; } = new();
    }

    public class PriceListUpdateRequestDto
    {
        public string? PriceListNumber { get; set; }
        public string PriceListName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CustomerCategory { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public string EffectiveFrom { get; set; } = string.Empty;
        public string? EffectiveTo { get; set; }
        public string? Remarks { get; set; }
        public List<PriceListItemRequestDto> Items { get; set; } = new();
    }

    public class PriceListResolveRequestDto
    {
        public string CustomerCategory { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string? Currency { get; set; }
        public string? EffectiveDate { get; set; }
    }

    public class PriceListResolveDto
    {
        public int PriceListId { get; set; }
        public string PriceListNumber { get; set; } = string.Empty;
        public string PriceListName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal TaxPercentage { get; set; }
        public string EffectiveFrom { get; set; } = string.Empty;
        public string? EffectiveTo { get; set; }
    }

    public class PriceListCompareDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public List<PriceListCompareEntryDto> Entries { get; set; } = new();
    }

    public class PriceListCompareEntryDto
    {
        public int PriceListId { get; set; }
        public string PriceListNumber { get; set; } = string.Empty;
        public string PriceListName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal MinimumPrice { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PriceListDashboardDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int ActiveCount { get; set; }
        public int ExpiredCount { get; set; }
        public int ArchivedCount { get; set; }
        public int TotalItems { get; set; }
        public List<PriceListListItemDto> Recent { get; set; } = new();
    }

    public class PriceListReportDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public List<PriceListListItemDto> Rows { get; set; } = new();
    }

    public class PriceListExportRequestDto
    {
        public string Format { get; set; } = "csv";
        public string? Status { get; set; }
        public string? CustomerCategory { get; set; }
    }

    public class PriceListExportMetadataDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string GeneratedOn { get; set; } = string.Empty;
    }

    public class PriceListListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? CustomerCategory { get; set; }
        public string? Currency { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class PriceListRemarksRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class PriceListLookupDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
