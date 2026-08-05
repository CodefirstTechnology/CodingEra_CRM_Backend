using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Infrastructure.Sales
{
    public class PriceListService : IPriceListService
    {
        private readonly IPriceListRepository _repo;
        private readonly IPriceListNumberingService _numbering;

        public PriceListService(
            IPriceListRepository repo,
            IPriceListNumberingService numbering)
        {
            _repo = repo;
            _numbering = numbering;
        }

        public async Task<IReadOnlyList<PriceListListItemDto>> GetAllAsync(
            PriceListListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(query, cancellationToken);
            return rows.Select(ApplyExpiry).Select(PriceListMapper.ToListItem).ToList();
        }

        public async Task<PriceListDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity is null ? null : PriceListMapper.ToDto(ApplyExpiry(entity));
        }

        public async Task<PriceListDto> CreateAsync(
            PriceListCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var (category, from, to, status) = ValidateAndNormalizeCreate(request);

            if (status == PriceListStatuses.Active)
            {
                await EnsureNoOverlapAsync(
                    request.PriceListName.Trim(),
                    category,
                    request.Currency.Trim().ToUpperInvariant(),
                    from,
                    to,
                    null,
                    cancellationToken);
            }

            var number = string.IsNullOrWhiteSpace(request.PriceListNumber)
                ? await _numbering.GenerateNextPriceListNumberAsync(cancellationToken)
                : request.PriceListNumber.Trim();

            if (await _repo.PriceListNumberExistsAsync(number, null, cancellationToken))
            {
                throw new InvalidOperationException($"Price list number '{number}' already exists.");
            }

            var now = DateTimeOffset.UtcNow;
            var entity = new PriceList
            {
                PriceListNumber = number,
                PriceListName = request.PriceListName.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                CustomerCategory = category,
                Currency = request.Currency.Trim().ToUpperInvariant(),
                EffectiveFrom = from,
                EffectiveTo = to,
                Status = status,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                Items = BuildItems(request.Items),
                History =
                [
                    NewHistory(PriceListHistoryActions.Created, "Price list created", actingUser, now)
                ]
            };

            if (status == PriceListStatuses.Active)
            {
                entity.History.Add(NewHistory(
                    PriceListHistoryActions.Activated,
                    "Activated on create",
                    actingUser,
                    now));
            }

            await _repo.CreateAsync(entity, cancellationToken);
            return PriceListMapper.ToDto(
                await _repo.GetByIdAsync(entity.Id, true, false, cancellationToken) ?? entity);
        }

        public async Task<PriceListDto?> UpdateAsync(
            int id,
            PriceListUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            ApplyExpiry(entity);
            if (PriceListStatusRules.IsReadOnly(entity.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot update a price list in '{entity.Status}' status.");
            }

            ValidateCore(
                request.PriceListName,
                request.Currency,
                request.EffectiveFrom,
                request.EffectiveTo,
                request.CustomerCategory,
                request.Items);

            var category = PriceListCustomerCategoryRules.Normalize(request.CustomerCategory)!;
            var from = PriceListMapper.ParseDate(request.EffectiveFrom, entity.EffectiveFrom);
            var to = PriceListMapper.ParseOptionalDate(request.EffectiveTo);
            if (to is not null && to < from)
            {
                throw new InvalidOperationException("Effective to must be on or after effective from.");
            }

            if (entity.Status == PriceListStatuses.Active)
            {
                await EnsureNoOverlapAsync(
                    request.PriceListName.Trim(),
                    category,
                    request.Currency.Trim().ToUpperInvariant(),
                    from,
                    to,
                    id,
                    cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(request.PriceListNumber)
                && !string.Equals(request.PriceListNumber.Trim(), entity.PriceListNumber, StringComparison.Ordinal))
            {
                var newNumber = request.PriceListNumber.Trim();
                if (await _repo.PriceListNumberExistsAsync(newNumber, id, cancellationToken))
                {
                    throw new InvalidOperationException($"Price list number '{newNumber}' already exists.");
                }

                entity.PriceListNumber = newNumber;
            }

            var now = DateTimeOffset.UtcNow;
            entity.PriceListName = request.PriceListName.Trim();
            entity.Description = request.Description?.Trim() ?? string.Empty;
            entity.CustomerCategory = category;
            entity.Currency = request.Currency.Trim().ToUpperInvariant();
            entity.EffectiveFrom = from;
            entity.EffectiveTo = to;
            entity.Remarks = request.Remarks?.Trim() ?? string.Empty;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.Items.Clear();
            foreach (var item in BuildItems(request.Items))
            {
                entity.Items.Add(item);
            }

            entity.History.Add(NewHistory(
                PriceListHistoryActions.Updated,
                "Price list updated",
                actingUser,
                now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return PriceListMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<bool> DeleteAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return false;
            }

            if (entity.Status != PriceListStatuses.Draft)
            {
                throw new InvalidOperationException("Only Draft price lists can be deleted.");
            }

            var now = DateTimeOffset.UtcNow;
            entity.IsDeleted = true;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.History.Add(NewHistory(
                PriceListHistoryActions.Deleted,
                "Price list deleted",
                actingUser,
                now));

            return await _repo.DeleteAsync(entity, cancellationToken);
        }

        public async Task<PriceListDto?> ActivateAsync(
            int id,
            PriceListRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (!PriceListStatusRules.CanTransition(entity.Status, PriceListStatuses.Active))
            {
                throw new InvalidOperationException(
                    $"Cannot activate price list from '{entity.Status}'.");
            }

            await EnsureNoOverlapAsync(
                entity.PriceListName,
                entity.CustomerCategory,
                entity.Currency,
                entity.EffectiveFrom,
                entity.EffectiveTo,
                id,
                cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.Status = PriceListStatuses.Active;
            entity.Remarks = request?.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.History.Add(NewHistory(
                PriceListHistoryActions.Activated,
                $"{old} → Active" + (string.IsNullOrWhiteSpace(request?.Remarks)
                    ? string.Empty
                    : $": {request.Remarks.Trim()}"),
                actingUser,
                now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return PriceListMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<PriceListDto?> CloneAsync(
            int id,
            PriceListRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var source = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            if (source is null)
            {
                return null;
            }

            var create = new PriceListCreateRequestDto
            {
                PriceListName = $"{source.PriceListName} (Copy)",
                Description = source.Description,
                CustomerCategory = source.CustomerCategory,
                Currency = source.Currency,
                EffectiveFrom = PriceListMapper.FormatDate(DateOnly.FromDateTime(DateTime.UtcNow)),
                EffectiveTo = source.EffectiveTo is null
                    ? null
                    : PriceListMapper.FormatDate(source.EffectiveTo.Value),
                Status = PriceListStatuses.Draft,
                Remarks = string.IsNullOrWhiteSpace(request?.Remarks)
                    ? $"Cloned from {source.PriceListNumber}"
                    : request.Remarks.Trim(),
                Items = source.Items.OrderBy(i => i.SortOrder).Select(i => new PriceListItemRequestDto
                {
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
                }).ToList()
            };

            var created = await CreateAsync(create, actingUser, cancellationToken);

            var entity = await _repo.GetByIdAsync(created.Id, true, true, cancellationToken);
            if (entity is not null)
            {
                entity.History.Add(NewHistory(
                    PriceListHistoryActions.Cloned,
                    $"Cloned from {source.PriceListNumber}",
                    actingUser,
                    DateTimeOffset.UtcNow));
                await _repo.UpdateAsync(entity, cancellationToken);
                return PriceListMapper.ToDto(
                    await _repo.GetByIdAsync(created.Id, true, false, cancellationToken) ?? entity);
            }

            return created;
        }

        public async Task<IReadOnlyList<PriceListListItemDto>> GetActiveAsync(
            string? asOfDate,
            CancellationToken cancellationToken = default)
        {
            var date = PriceListMapper.ParseOptionalDate(asOfDate)
                ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var rows = await _repo.GetActiveAsync(date, cancellationToken);
            return rows.Select(PriceListMapper.ToListItem).ToList();
        }

        public async Task<PriceListResolveDto?> ResolveAsync(
            PriceListResolveRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerCategory))
            {
                throw new InvalidOperationException("Customer category is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ItemCode))
            {
                throw new InvalidOperationException("Item code is required.");
            }

            var category = PriceListCustomerCategoryRules.Normalize(request.CustomerCategory)
                ?? throw new InvalidOperationException($"Unknown customer category '{request.CustomerCategory}'.");

            var date = PriceListMapper.ParseOptionalDate(request.EffectiveDate)
                ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var currency = string.IsNullOrWhiteSpace(request.Currency)
                ? null
                : request.Currency.Trim().ToUpperInvariant();

            var active = await _repo.GetActiveAsync(date, cancellationToken);
            var match = active
                .Where(x => x.CustomerCategory == category
                    && (currency is null || x.Currency == currency))
                .SelectMany(list => list.Items
                    .Where(i => i.ItemCode.Equals(request.ItemCode.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Select(item => new { List = list, Item = item }))
                .OrderByDescending(x => x.List.EffectiveFrom)
                .ThenByDescending(x => x.List.Id)
                .FirstOrDefault();

            if (match is null)
            {
                return null;
            }

            return new PriceListResolveDto
            {
                PriceListId = match.List.Id,
                PriceListNumber = match.List.PriceListNumber,
                PriceListName = match.List.PriceListName,
                CustomerCategory = match.List.CustomerCategory,
                Currency = match.List.Currency,
                ItemCode = match.Item.ItemCode,
                ItemName = match.Item.ItemName,
                BasePrice = match.Item.BasePrice,
                SellingPrice = match.Item.SellingPrice,
                DiscountPercentage = match.Item.DiscountPercentage,
                MinimumPrice = match.Item.MinimumPrice,
                TaxPercentage = match.Item.TaxPercentage,
                EffectiveFrom = PriceListMapper.FormatDate(match.List.EffectiveFrom),
                EffectiveTo = PriceListMapper.FormatOptionalDate(match.List.EffectiveTo)
            };
        }

        public async Task<IReadOnlyList<PriceListCompareDto>> CompareAsync(
            string? itemCode,
            string? customerCategory,
            CancellationToken cancellationToken = default)
        {
            var lists = await _repo.GetAllAsync(new PriceListListQueryDto
            {
                Status = PriceListStatuses.Active,
                CustomerCategory = customerCategory
            }, cancellationToken);

            var items = lists
                .SelectMany(list => list.Items.Select(item => new { List = list, Item = item }))
                .Where(x => string.IsNullOrWhiteSpace(itemCode)
                    || x.Item.ItemCode.Equals(itemCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Item.ItemCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => new PriceListCompareDto
                {
                    ItemCode = g.Key,
                    ItemName = g.First().Item.ItemName,
                    Entries = g.Select(x => new PriceListCompareEntryDto
                    {
                        PriceListId = x.List.Id,
                        PriceListNumber = x.List.PriceListNumber,
                        PriceListName = x.List.PriceListName,
                        CustomerCategory = x.List.CustomerCategory,
                        Currency = x.List.Currency,
                        SellingPrice = x.Item.SellingPrice,
                        DiscountPercentage = x.Item.DiscountPercentage,
                        MinimumPrice = x.Item.MinimumPrice,
                        Status = x.List.Status
                    }).OrderBy(e => e.SellingPrice).ToList()
                })
                .OrderBy(x => x.ItemCode)
                .ToList();

            return items;
        }

        public async Task<PriceListDashboardDto> GetDashboardAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = (await _repo.GetAllAsync(null, cancellationToken))
                .Select(ApplyExpiry).ToList();

            return new PriceListDashboardDto
            {
                TotalCount = rows.Count,
                DraftCount = rows.Count(x => x.Status == PriceListStatuses.Draft),
                ActiveCount = rows.Count(x => x.Status == PriceListStatuses.Active),
                ExpiredCount = rows.Count(x => x.Status == PriceListStatuses.Expired),
                ArchivedCount = rows.Count(x => x.Status == PriceListStatuses.Archived),
                TotalItems = rows.Sum(x => x.Items.Count),
                Recent = rows.OrderByDescending(x => x.UpdatedDate).Take(10)
                    .Select(PriceListMapper.ToListItem).ToList()
            };
        }

        public async Task<PriceListReportDto> GetReportsAsync(
            PriceListListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await GetAllAsync(query, cancellationToken);
            return new PriceListReportDto
            {
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O"),
                RowCount = rows.Count,
                Rows = rows.ToList()
            };
        }

        public async Task<PriceListExportMetadataDto> ExportReportsAsync(
            PriceListExportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var report = await GetReportsAsync(new PriceListListQueryDto
            {
                Status = request.Status,
                CustomerCategory = request.CustomerCategory
            }, cancellationToken);

            return new PriceListExportMetadataDto
            {
                FileName = $"price-list-report-{DateTime.UtcNow:yyyyMMddHHmmss}.{(request.Format?.ToLowerInvariant() == "xlsx" ? "xlsx" : "csv")}",
                ContentType = request.Format?.ToLowerInvariant() == "xlsx"
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "text/csv",
                Message = $"Export prepared for {report.RowCount} row(s) (placeholder).",
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O")
            };
        }

        public async Task<IReadOnlyList<PriceListHistoryDto>?> GetHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, false, false, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            var rows = await _repo.GetHistoryAsync(id, cancellationToken);
            return rows.Select(PriceListMapper.ToHistoryDto).ToList();
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(
            [
                "price-lists.view",
                "price-lists.create",
                "price-lists.edit",
                "price-lists.delete",
                "price-lists.activate",
                "price-lists.clone",
                "price-lists.resolve",
                "price-lists.dashboard.view",
                "price-lists.report.view"
            ]);

        public async Task<IReadOnlyList<PriceListLookupDto>> LookupItemsAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(null, cancellationToken);
            return rows.SelectMany(x => x.Items)
                .GroupBy(x => x.ItemCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(x => x.ItemCode)
                .Take(200)
                .Select(x => new PriceListLookupDto
                {
                    Id = x.ItemCode,
                    Name = $"{x.ItemCode} — {x.ItemName}"
                })
                .ToList();
        }

        public Task<IReadOnlyList<PriceListLookupDto>> LookupCustomerCategoriesAsync() =>
            Task.FromResult<IReadOnlyList<PriceListLookupDto>>(
                PriceListCustomerCategories.All
                    .Select(c => new PriceListLookupDto { Id = c, Name = c })
                    .ToList());

        public async Task<IReadOnlyList<PriceListLookupDto>> LookupCurrenciesAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(null, cancellationToken);
            var currencies = rows.Select(x => x.Currency)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (currencies.Count == 0)
            {
                currencies = ["INR", "USD", "EUR"];
            }

            return currencies.Select(c => new PriceListLookupDto { Id = c, Name = c }).ToList();
        }

        private async Task EnsureNoOverlapAsync(
            string name,
            string category,
            string currency,
            DateOnly from,
            DateOnly? to,
            int? excludeId,
            CancellationToken cancellationToken)
        {
            if (await _repo.HasOverlappingActiveAsync(
                    name, category, currency, from, to, excludeId, cancellationToken))
            {
                throw new InvalidOperationException(
                    "An overlapping active price list already exists for the same name, customer category, currency, and period.");
            }
        }

        private static PriceList ApplyExpiry(PriceList entity)
        {
            if (entity.Status == PriceListStatuses.Active
                && entity.EffectiveTo is DateOnly end
                && end < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                entity.Status = PriceListStatuses.Expired;
            }

            return entity;
        }

        private static (string Category, DateOnly From, DateOnly? To, string Status)
            ValidateAndNormalizeCreate(PriceListCreateRequestDto request)
        {
            ValidateCore(
                request.PriceListName,
                request.Currency,
                request.EffectiveFrom,
                request.EffectiveTo,
                request.CustomerCategory,
                request.Items);

            var category = PriceListCustomerCategoryRules.Normalize(request.CustomerCategory)
                ?? throw new InvalidOperationException($"Unknown customer category '{request.CustomerCategory}'.");

            var from = PriceListMapper.ParseDate(
                request.EffectiveFrom,
                DateOnly.FromDateTime(DateTime.UtcNow));
            var to = PriceListMapper.ParseOptionalDate(request.EffectiveTo);
            if (to is not null && to < from)
            {
                throw new InvalidOperationException("Effective to must be on or after effective from.");
            }

            var status = PriceListStatusRules.Normalize(request.Status ?? PriceListStatuses.Draft)
                ?? PriceListStatuses.Draft;
            if (status is not (PriceListStatuses.Draft or PriceListStatuses.Active))
            {
                throw new InvalidOperationException("New price lists may only start as Draft or Active.");
            }

            return (category, from, to, status);
        }

        private static void ValidateCore(
            string name,
            string currency,
            string effectiveFrom,
            string? effectiveTo,
            string customerCategory,
            IList<PriceListItemRequestDto> items)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Price list name is required.");
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new InvalidOperationException("Currency is required.");
            }

            if (string.IsNullOrWhiteSpace(effectiveFrom))
            {
                throw new InvalidOperationException("Effective from date is required.");
            }

            if (string.IsNullOrWhiteSpace(customerCategory))
            {
                throw new InvalidOperationException("Customer category is required.");
            }

            if (items is null || items.Count == 0)
            {
                throw new InvalidOperationException("At least one price list item is required.");
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemCode))
                {
                    throw new InvalidOperationException("Item code is required.");
                }

                if (string.IsNullOrWhiteSpace(item.ItemName))
                {
                    throw new InvalidOperationException("Item name is required.");
                }

                if (item.SellingPrice < 0)
                {
                    throw new InvalidOperationException("Selling price cannot be negative.");
                }

                if (item.MinimumPrice < 0)
                {
                    throw new InvalidOperationException("Minimum price cannot be negative.");
                }

                if (item.SellingPrice < item.MinimumPrice)
                {
                    throw new InvalidOperationException(
                        $"Selling price for '{item.ItemCode}' cannot be below minimum price.");
                }

                if (item.DiscountPercentage is < 0 or > 100)
                {
                    throw new InvalidOperationException("Discount percentage must be between 0 and 100.");
                }

                if (item.MaximumDiscount is < 0 or > 100)
                {
                    throw new InvalidOperationException("Maximum discount must be between 0 and 100.");
                }

                if (item.DiscountPercentage > item.MaximumDiscount && item.MaximumDiscount > 0)
                {
                    throw new InvalidOperationException(
                        $"Discount for '{item.ItemCode}' exceeds maximum discount.");
                }

                if (item.TaxPercentage is < 0 or > 100)
                {
                    throw new InvalidOperationException("Tax percentage must be between 0 and 100.");
                }
            }
        }

        private static List<PriceListItem> BuildItems(IList<PriceListItemRequestDto> items)
        {
            var list = new List<PriceListItem>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                list.Add(new PriceListItem
                {
                    ItemCode = item.ItemCode.Trim(),
                    ItemName = item.ItemName.Trim(),
                    ItemCategory = item.ItemCategory?.Trim() ?? string.Empty,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Nos" : item.Unit.Trim(),
                    BasePrice = PriceListCalculator.Round2(item.BasePrice),
                    SellingPrice = PriceListCalculator.Round2(item.SellingPrice),
                    DiscountPercentage = item.DiscountPercentage,
                    MinimumPrice = PriceListCalculator.Round2(item.MinimumPrice),
                    MaximumDiscount = item.MaximumDiscount,
                    TaxPercentage = item.TaxPercentage,
                    Remarks = item.Remarks?.Trim() ?? string.Empty,
                    SortOrder = item.SortOrder ?? i
                });
            }

            return list;
        }

        private static PriceListHistory NewHistory(
            string action,
            string remarks,
            string changedBy,
            DateTimeOffset changedOn) => new()
        {
            Action = action,
            Remarks = remarks,
            ChangedBy = changedBy,
            ChangedOn = changedOn
        };
    }
}
