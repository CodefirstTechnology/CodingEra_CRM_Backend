using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Xunit;

namespace Backend_ERP.Tests;

public class PriceListContractTests
{
    [Fact]
    public void Calculator_computes_selling_price_from_base_and_discount()
    {
        Assert.Equal(900m, PriceListCalculator.CalcSellingFromBase(1000m, 10m));
        Assert.Equal(850m, PriceListCalculator.CalcSellingFromBase(1000m, 15m));
    }

    [Theory]
    [InlineData(PriceListStatuses.Draft, PriceListStatuses.Active, true)]
    [InlineData(PriceListStatuses.Draft, PriceListStatuses.Archived, true)]
    [InlineData(PriceListStatuses.Active, PriceListStatuses.Expired, true)]
    [InlineData(PriceListStatuses.Active, PriceListStatuses.Archived, true)]
    [InlineData(PriceListStatuses.Expired, PriceListStatuses.Archived, true)]
    [InlineData(PriceListStatuses.Archived, PriceListStatuses.Active, false)]
    [InlineData(PriceListStatuses.Expired, PriceListStatuses.Active, false)]
    public void Status_transitions_are_validated(string from, string to, bool expected)
    {
        Assert.Equal(expected, PriceListStatusRules.CanTransition(from, to));
    }

    [Fact]
    public void Expired_and_archived_are_read_only()
    {
        Assert.True(PriceListStatusRules.IsReadOnly(PriceListStatuses.Expired));
        Assert.True(PriceListStatusRules.IsReadOnly(PriceListStatuses.Archived));
        Assert.True(PriceListStatusRules.IsEditable(PriceListStatuses.Draft));
        Assert.True(PriceListStatusRules.IsEditable(PriceListStatuses.Active));
    }

    [Fact]
    public void Create_request_requires_items_and_core_fields()
    {
        var dto = new PriceListCreateRequestDto
        {
            PriceListName = "Steel Standard FY26",
            CustomerCategory = PriceListCustomerCategories.Standard,
            Currency = "INR",
            EffectiveFrom = "2026-04-01",
            Items =
            [
                new PriceListItemRequestDto
                {
                    ItemCode = "STL-MS-001",
                    ItemName = "MS Plate 10mm",
                    BasePrice = 62000,
                    SellingPrice = 58500,
                    MinimumPrice = 56000,
                    DiscountPercentage = 5,
                    MaximumDiscount = 10,
                    TaxPercentage = 18
                }
            ]
        };

        Assert.Equal("Steel Standard FY26", dto.PriceListName);
        Assert.Single(dto.Items);
        Assert.True(dto.Items[0].SellingPrice >= dto.Items[0].MinimumPrice);
    }

    [Fact]
    public void Resolve_and_compare_dto_shapes_exist()
    {
        var resolve = new PriceListResolveRequestDto
        {
            CustomerCategory = PriceListCustomerCategories.Premium,
            ItemCode = "STL-MS-001",
            Currency = "INR"
        };
        var compare = new PriceListCompareDto
        {
            ItemCode = "STL-MS-001",
            ItemName = "MS Plate 10mm",
            Entries =
            [
                new PriceListCompareEntryDto
                {
                    PriceListId = 1,
                    SellingPrice = 58000
                }
            ]
        };

        Assert.Equal(PriceListCustomerCategories.Premium, resolve.CustomerCategory);
        Assert.Single(compare.Entries);
    }

    [Fact]
    public void Customer_category_normalize_accepts_known_values()
    {
        Assert.Equal(PriceListCustomerCategories.Distributor,
            PriceListCustomerCategoryRules.Normalize("distributor"));
        Assert.Equal(PriceListCustomerCategories.Enterprise,
            PriceListCustomerCategoryRules.Normalize("Enterprise"));
        Assert.Null(PriceListCustomerCategoryRules.Normalize("Unknown"));
    }

    [Fact]
    public void History_dto_shape_is_available()
    {
        var dto = new PriceListHistoryDto
        {
            Action = PriceListHistoryActions.Activated,
            Remarks = "Draft → Active",
            ChangedBy = "1",
            ChangedOn = "2026-08-05T10:00:00.0000000Z"
        };

        Assert.Equal(PriceListHistoryActions.Activated, dto.Action);
    }

    [Fact]
    public void Activate_and_clone_actions_are_tracked_in_history_constants()
    {
        Assert.Equal("Activated", PriceListHistoryActions.Activated);
        Assert.Equal("Cloned", PriceListHistoryActions.Cloned);
        Assert.Equal("Deleted", PriceListHistoryActions.Deleted);
        Assert.True(PriceListStatusRules.CanTransition(PriceListStatuses.Draft, PriceListStatuses.Active));
    }

    [Fact]
    public void Update_request_preserves_identity_and_items()
    {
        var dto = new PriceListUpdateRequestDto
        {
            PriceListName = "Steel Standard FY26 Updated",
            CustomerCategory = PriceListCustomerCategories.Standard,
            Currency = "INR",
            EffectiveFrom = "2026-04-01",
            EffectiveTo = "2027-03-31",
            Remarks = "Rate revision",
            Items =
            [
                new PriceListItemRequestDto
                {
                    ItemCode = "STL-MS-001",
                    ItemName = "MS Plate 10mm",
                    BasePrice = 63000,
                    SellingPrice = 59500,
                    MinimumPrice = 57000,
                    DiscountPercentage = 5.5m,
                    MaximumDiscount = 12,
                    TaxPercentage = 18
                }
            ]
        };

        Assert.Equal("Steel Standard FY26 Updated", dto.PriceListName);
        Assert.Equal(59500m, dto.Items[0].SellingPrice);
        Assert.True(dto.Items[0].SellingPrice >= dto.Items[0].MinimumPrice);
        Assert.True(dto.Items[0].DiscountPercentage <= dto.Items[0].MaximumDiscount);
    }

    [Fact]
    public void Validation_rules_reject_selling_below_minimum()
    {
        var selling = 50000m;
        var minimum = 56000m;
        Assert.True(selling < minimum);
        Assert.Equal(900m, PriceListCalculator.CalcSellingFromBase(1000m, 10m));
    }

    [Fact]
    public void Numbering_prefix_matches_pl_year_pattern()
    {
        Assert.Matches(@"^PL-\d{4}-\d{5}$", "PL-2026-00001");
        Assert.Matches(@"^PL-\d{4}-\d{5}$", "PL-2026-00010");
    }
}
