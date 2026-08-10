using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;

namespace ERP.Infrastructure.Sales
{
    internal static class PriceListMapper
    {
        // Delegated to shared DateHelper — kept for backward-compat callsites in this class
        private static string FormatDate(DateOnly date) => DateHelper.FormatDate(date);
        public static string? FormatOptionalDate(DateOnly? date) =>
            date is null ? null : DateHelper.FormatDate(date.Value);
        private static string FormatDateTime(DateTimeOffset value) => DateHelper.FormatDateTime(value);
        public static DateOnly ParseDate(string value, DateOnly fallback) => DateHelper.ParseDate(value, fallback);

        public static DateOnly? ParseOptionalDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (DateOnly.TryParse(value, out var date)) return date;
            if (DateTimeOffset.TryParse(value, out var dto)) return DateOnly.FromDateTime(dto.UtcDateTime);
            return null;
        }

        public static PriceListListItemDto ToListItem(PriceList e) => new()
        {
            Id = e.Id,
            PriceListNumber = e.PriceListNumber,
            PriceListName = e.PriceListName,
            CustomerCategory = e.CustomerCategory,
            Currency = e.Currency,
            EffectiveFrom = FormatDate(e.EffectiveFrom),
            EffectiveTo = FormatOptionalDate(e.EffectiveTo),
            Status = e.Status,
            ItemCount = e.Items?.Count ?? 0
        };

        public static PriceListItemDto ToItemDto(PriceListItem i) => new()
        {
            Id = i.Id,
            ItemCode = i.ItemCode,
            ItemName = i.ItemName,
            ItemCategory = i.ItemCategory,
            Unit = i.Unit,
            BasePrice = i.BasePrice,
            SellingPrice = i.SellingPrice,
            DiscountPercentage = i.DiscountPercentage,
            MinimumPrice = i.MinimumPrice,
            MaximumDiscount = i.MaximumDiscount,
            TaxPercentage = i.TaxPercentage,
            Remarks = i.Remarks,
            SortOrder = i.SortOrder
        };

        public static PriceListHistoryDto ToHistoryDto(PriceListHistory h) => new()
        {
            Id = h.Id,
            Action = h.Action,
            Remarks = h.Remarks,
            ChangedBy = h.ChangedBy,
            ChangedOn = FormatDateTime(h.ChangedOn)
        };

        public static PriceListDto ToDto(PriceList e) => new()
        {
            Id = e.Id,
            PriceListNumber = e.PriceListNumber,
            PriceListName = e.PriceListName,
            Description = e.Description,
            CustomerCategory = e.CustomerCategory,
            Currency = e.Currency,
            EffectiveFrom = FormatDate(e.EffectiveFrom),
            EffectiveTo = FormatOptionalDate(e.EffectiveTo),
            Status = e.Status,
            Remarks = e.Remarks,
            CreatedBy = e.CreatedBy,
            CreatedDate = FormatDateTime(e.CreatedDate),
            UpdatedBy = e.UpdatedBy,
            UpdatedDate = FormatDateTime(e.UpdatedDate),
            Items = e.Items.OrderBy(i => i.SortOrder).Select(ToItemDto).ToList(),
            History = e.History.OrderBy(h => h.ChangedOn).Select(ToHistoryDto).ToList()
        };
    }
}
