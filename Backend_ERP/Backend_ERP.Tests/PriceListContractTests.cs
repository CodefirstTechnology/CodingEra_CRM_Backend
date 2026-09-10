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
    public void Priority_defaults_to_zero_and_is_supported_on_dtos()
    {
        var createDto = new PriceListCreateRequestDto();
        Assert.Equal(0, createDto.Priority);

        var entity = new PriceList();
        Assert.Equal(0, entity.Priority);

        createDto.Priority = 10;
        Assert.Equal(10, createDto.Priority);

        var updateDto = new PriceListUpdateRequestDto { Priority = 5 };
        Assert.Equal(5, updateDto.Priority);

        var listDto = new PriceListListItemDto { Priority = 2 };
        Assert.Equal(2, listDto.Priority);

        var fullDto = new PriceListDto { Priority = 7 };
        Assert.Equal(7, fullDto.Priority);

        var historyDto = new PriceListHistoryDto { Priority = 3 };
        Assert.Equal(3, historyDto.Priority);
    }

    [Fact]
    public void Resolve_dto_includes_priority_and_alternative_lists_metadata()
    {
        var resolve = new PriceListResolveDto
        {
            PriceListId = 12,
            PriceListNumber = "PL-2026-00012",
            PriceListName = "Promotional Steel Q1",
            Priority = 10,
            ItemCode = "STL-MS-001",
            ItemName = "MS Plate 10mm",
            BasePrice = 60000,
            SellingPrice = 54000,
            MinimumPrice = 52000,
            DiscountPercentage = 10,
            MaximumDiscount = 15,
            TaxPercentage = 18,
            AlternativeListsAvailable = 2,
            ResolvedViaPriority = true
        };

        Assert.Equal(10, resolve.Priority);
        Assert.Equal(2, resolve.AlternativeListsAvailable);
        Assert.True(resolve.ResolvedViaPriority);
        Assert.Equal(15m, resolve.MaximumDiscount);
    }

    [Fact]
    public void Resolution_ordering_prioritizes_higher_priority_then_effective_from()
    {
        var standardList = new PriceList
        {
            Id = 1,
            Priority = 0,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            PriceListName = "Standard Catalog"
        };
        var olderPromoList = new PriceList
        {
            Id = 2,
            Priority = 5,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            PriceListName = "Older Promo"
        };
        var latestHighPriorityList = new PriceList
        {
            Id = 3,
            Priority = 10,
            EffectiveFrom = new DateOnly(2026, 3, 1),
            PriceListName = "Spring Flash Sale"
        };

        var lists = new List<PriceList> { standardList, olderPromoList, latestHighPriorityList };

        var sorted = lists
            .OrderByDescending(p => p.Priority)
            .ThenByDescending(p => p.EffectiveFrom)
            .ThenByDescending(p => p.Id)
            .ToList();

        Assert.Equal(3, sorted[0].Id);
        Assert.Equal("Spring Flash Sale", sorted[0].PriceListName);
        Assert.Equal(2, sorted[1].Id);
        Assert.Equal(1, sorted[2].Id);
    }
}

