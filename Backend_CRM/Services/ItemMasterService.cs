using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public sealed class ItemMasterService : IItemMasterService
    {
        private readonly TaskDbcontext _db;

        public ItemMasterService(TaskDbcontext db)
        {
            _db = db;
        }

        public async Task<List<ItemGroupDto>> ListGroupsAsync(bool activeOnly = false, CancellationToken ct = default)
        {
            var query = _db.ItemGroups.AsNoTracking();
            if (activeOnly)
            {
                query = query.Where(g => g.IsActive);
            }

            var groups = await query.OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToListAsync(ct);
            var itemCounts = await _db.Items.AsNoTracking()
                .Where(i => i.ParentItemId == null && i.ItemGroupId != null)
                .GroupBy(i => i.ItemGroupId!.Value)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

            var childCounts = groups
                .Where(g => g.ParentId.HasValue)
                .GroupBy(g => g.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var parentNames = groups.ToDictionary(g => g.Id, g => g.Name);

            return groups.Select(g =>
            {
                var parentName = g.ParentId.HasValue && parentNames.TryGetValue(g.ParentId.Value, out var pn) ? pn : null;
                itemCounts.TryGetValue(g.Id, out var ic);
                childCounts.TryGetValue(g.Id, out var cc);
                return ItemMasterMappingHelper.ToGroupDto(g, ic, cc, parentName);
            }).ToList();
        }

        public async Task<ItemGroupDto?> GetGroupAsync(int id, CancellationToken ct = default)
        {
            var g = await _db.ItemGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (g == null) return null;

            var itemCount = await _db.Items.CountAsync(i => i.ItemGroupId == id && i.ParentItemId == null, ct);
            var childCount = await _db.ItemGroups.CountAsync(c => c.ParentId == id, ct);
            string? parentName = null;
            if (g.ParentId.HasValue)
            {
                parentName = await _db.ItemGroups.AsNoTracking()
                    .Where(p => p.Id == g.ParentId.Value)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync(ct);
            }

            return ItemMasterMappingHelper.ToGroupDto(g, itemCount, childCount, parentName);
        }

        public async Task<ItemGroupDto> CreateGroupAsync(ItemGroupUpsertDto dto, CancellationToken ct = default)
        {
            await ValidateGroupParentAsync(dto.ParentId, null, ct);
            var now = DateTime.UtcNow;
            var row = new ItemGroup
            {
                Name = dto.Name.Trim(),
                ParentId = dto.ParentId,
                Description = dto.Description?.Trim() ?? string.Empty,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.ItemGroups.Add(row);
            await _db.SaveChangesAsync(ct);
            return (await GetGroupAsync(row.Id, ct))!;
        }

        public async Task<ItemGroupDto?> UpdateGroupAsync(int id, ItemGroupUpsertDto dto, CancellationToken ct = default)
        {
            var row = await _db.ItemGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
            if (row == null) return null;

            await ValidateGroupParentAsync(dto.ParentId, id, ct);
            row.Name = dto.Name.Trim();
            row.ParentId = dto.ParentId;
            row.Description = dto.Description?.Trim() ?? string.Empty;
            row.SortOrder = dto.SortOrder;
            row.IsActive = dto.IsActive;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return await GetGroupAsync(id, ct);
        }

        public async Task<bool> DeleteGroupAsync(int id, CancellationToken ct = default)
        {
            var row = await _db.ItemGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
            if (row == null) return false;

            var hasChildren = await _db.ItemGroups.AnyAsync(g => g.ParentId == id, ct);
            if (hasChildren)
            {
                throw new InvalidOperationException("Cannot delete a group that has child groups.");
            }

            var hasItems = await _db.Items.AnyAsync(i => i.ItemGroupId == id, ct);
            if (hasItems)
            {
                throw new InvalidOperationException("Cannot delete a group that contains items.");
            }

            _db.ItemGroups.Remove(row);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<ItemAttributeDto>> ListAttributesAsync(bool activeOnly = false, CancellationToken ct = default)
        {
            IQueryable<ItemAttribute> query = _db.ItemAttributes.AsNoTracking().Include(a => a.Values);
            if (activeOnly)
            {
                query = query.Where(a => a.IsActive);
            }

            var rows = await query.OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync(ct);
            return rows.Select(ItemMasterMappingHelper.ToAttributeDto).ToList();
        }

        public async Task<ItemAttributeDto?> GetAttributeAsync(int id, CancellationToken ct = default)
        {
            var row = await _db.ItemAttributes.AsNoTracking()
                .Include(a => a.Values)
                .FirstOrDefaultAsync(a => a.Id == id, ct);
            return row == null ? null : ItemMasterMappingHelper.ToAttributeDto(row);
        }

        public async Task<ItemAttributeDto> CreateAttributeAsync(ItemAttributeUpsertDto dto, CancellationToken ct = default)
        {
            var code = string.IsNullOrWhiteSpace(dto.Code)
                ? ItemMasterMappingHelper.SlugifyCode(dto.Name)
                : dto.Code.Trim().ToLowerInvariant();
            await EnsureAttributeCodeUniqueAsync(code, null, ct);

            var now = DateTime.UtcNow;
            var row = new ItemAttribute
            {
                Name = dto.Name.Trim(),
                Code = code,
                ValueType = ItemMasterMappingHelper.ParseValueType(dto.ValueType),
                IsVariantAttribute = dto.IsVariantAttribute,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
            };
            ApplyAttributeValues(row, dto.Values);
            _db.ItemAttributes.Add(row);
            await _db.SaveChangesAsync(ct);
            return (await GetAttributeAsync(row.Id, ct))!;
        }

        public async Task<ItemAttributeDto?> UpdateAttributeAsync(int id, ItemAttributeUpsertDto dto, CancellationToken ct = default)
        {
            var row = await _db.ItemAttributes.Include(a => a.Values).FirstOrDefaultAsync(a => a.Id == id, ct);
            if (row == null) return null;

            var code = string.IsNullOrWhiteSpace(dto.Code)
                ? ItemMasterMappingHelper.SlugifyCode(dto.Name)
                : dto.Code.Trim().ToLowerInvariant();
            await EnsureAttributeCodeUniqueAsync(code, id, ct);

            row.Name = dto.Name.Trim();
            row.Code = code;
            row.ValueType = ItemMasterMappingHelper.ParseValueType(dto.ValueType);
            row.IsVariantAttribute = dto.IsVariantAttribute;
            row.SortOrder = dto.SortOrder;
            row.IsActive = dto.IsActive;
            row.UpdatedAt = DateTime.UtcNow;
            SyncAttributeValues(row, dto.Values);
            await _db.SaveChangesAsync(ct);
            return await GetAttributeAsync(id, ct);
        }

        public async Task<bool> DeleteAttributeAsync(int id, CancellationToken ct = default)
        {
            var row = await _db.ItemAttributes.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (row == null) return false;

            var inUse = await _db.ItemVariantAttributeValues.AnyAsync(v => v.AttributeId == id, ct)
                || await _db.ItemTemplateAttributes.AnyAsync(t => t.AttributeId == id, ct);
            if (inUse)
            {
                throw new InvalidOperationException("Cannot delete an attribute that is in use by items.");
            }

            _db.ItemAttributes.Remove(row);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<PagedResultDto<ItemListItemDto>> ListItemsAsync(ItemListQueryDto query, CancellationToken ct = default)
        {
            var q = _db.Items.AsNoTracking()
                .Include(i => i.ItemGroup)
                .Include(i => i.ParentItem)
                .Include(i => i.VariantAttributeValues)
                    .ThenInclude(v => v.Attribute)
                .Include(i => i.VariantAttributeValues)
                    .ThenInclude(v => v.AttributeValue)
                .AsQueryable();

            if (!query.IncludeVariants)
            {
                q = q.Where(i => i.ParentItemId == null);
            }

            if (query.ParentItemId.HasValue)
            {
                q = q.Where(i => i.ParentItemId == query.ParentItemId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(i =>
                    i.ItemName.ToLower().Contains(s) ||
                    i.ItemCode.ToLower().Contains(s));
            }

            if (query.ItemGroupId.HasValue)
            {
                q = q.Where(i => i.ItemGroupId == query.ItemGroupId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = ItemMasterMappingHelper.ParseStatus(query.Status);
                q = q.Where(i => i.Status == status);
            }

            if (query.AttributeFilters != null && query.AttributeFilters.Count > 0)
            {
                foreach (var filter in query.AttributeFilters)
                {
                    if (!int.TryParse(filter.Key, out var attrId) || string.IsNullOrWhiteSpace(filter.Value))
                    {
                        continue;
                    }

                    var val = filter.Value.Trim().ToLower();
                    q = q.Where(i => i.VariantAttributeValues.Any(v =>
                        v.AttributeId == attrId &&
                        (v.CustomValue.ToLower().Contains(val) ||
                         (v.AttributeValue != null && v.AttributeValue.Value.ToLower().Contains(val)))));
                }
            }

            q = ApplyItemSort(q, query.SortBy, query.SortDir);

            var total = await q.CountAsync(ct);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var skip = (page - 1) * pageSize;

            var items = await q.Skip(skip).Take(pageSize).ToListAsync(ct);
            var parentIds = items.Where(i => i.HasVariants).Select(i => i.Id).ToList();
            var variantCounts = parentIds.Count == 0
                ? new Dictionary<int, int>()
                : await _db.Items.AsNoTracking()
                    .Where(v => v.ParentItemId != null && parentIds.Contains(v.ParentItemId.Value))
                    .GroupBy(v => v.ParentItemId!.Value)
                    .Select(g => new { ParentId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ParentId, x => x.Count, ct);

            var dtos = items.Select(i =>
            {
                variantCounts.TryGetValue(i.Id, out var vc);
                return ItemMasterMappingHelper.ToListItemDto(
                    i,
                    i.ItemGroup?.Name,
                    i.ParentItem?.ItemName,
                    vc,
                    i.VariantAttributeValues);
            }).ToList();

            return new PagedResultDto<ItemListItemDto>
            {
                Items = dtos,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            };
        }

        public async Task<ItemDetailDto?> GetItemAsync(int id, CancellationToken ct = default)
        {
            var item = await _db.Items.AsNoTracking()
                .Include(i => i.ItemGroup)
                .Include(i => i.ParentItem)
                .Include(i => i.Specifications)
                .Include(i => i.TemplateAttributes).ThenInclude(t => t.Attribute)
                .Include(i => i.VariantAttributeValues).ThenInclude(v => v.Attribute)
                .Include(i => i.VariantAttributeValues).ThenInclude(v => v.AttributeValue)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (item == null) return null;

            var detail = new ItemDetailDto
            {
                Id = item.Id,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                ItemGroupId = item.ItemGroupId,
                ItemGroupName = item.ItemGroup?.Name ?? string.Empty,
                Description = item.Description,
                Status = item.Status.ToString(),
                HasVariants = item.HasVariants,
                ParentItemId = item.ParentItemId,
                ParentItemName = item.ParentItem?.ItemName ?? string.Empty,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Specifications = item.Specifications
                    .OrderBy(s => s.SortOrder)
                    .Select(ItemMasterMappingHelper.ToSpecificationDto)
                    .ToList(),
                TemplateAttributes = item.TemplateAttributes
                    .Select(t => new ItemVariantAttributeDto
                    {
                        AttributeId = t.AttributeId,
                        AttributeName = t.Attribute.Name,
                        AttributeCode = t.Attribute.Code,
                        Value = string.Empty,
                    })
                    .ToList(),
                VariantAttributes = item.VariantAttributeValues
                    .Select(ItemMasterMappingHelper.ToVariantAttributeDto)
                    .ToList(),
            };

            if (item.HasVariants)
            {
                var variants = await _db.Items.AsNoTracking()
                    .Include(v => v.VariantAttributeValues).ThenInclude(a => a.Attribute)
                    .Include(v => v.VariantAttributeValues).ThenInclude(a => a.AttributeValue)
                    .Where(v => v.ParentItemId == item.Id)
                    .OrderBy(v => v.ItemCode)
                    .ToListAsync(ct);

                detail.VariantCount = variants.Count;
                detail.Variants = variants.Select(v => ItemMasterMappingHelper.ToListItemDto(
                    v,
                    item.ItemGroup?.Name,
                    item.ItemName,
                    0,
                    v.VariantAttributeValues)).ToList();
            }

            return detail;
        }

        public async Task<ItemDetailDto> CreateItemAsync(ItemUpsertDto dto, CancellationToken ct = default)
        {
            await EnsureItemCodeUniqueAsync(dto.ItemCode.Trim(), null, ct);
            var now = DateTime.UtcNow;
            var row = new Item
            {
                ItemCode = dto.ItemCode.Trim(),
                ItemName = dto.ItemName.Trim(),
                ItemGroupId = dto.ItemGroupId,
                Description = dto.Description?.Trim() ?? string.Empty,
                Status = ItemMasterMappingHelper.ParseStatus(dto.Status),
                HasVariants = dto.HasVariants,
                CreatedAt = now,
                UpdatedAt = now,
            };

            ApplySpecifications(row, dto.Specifications);
            ApplyTemplateAttributes(row, dto.VariantAttributeIds);
            _db.Items.Add(row);
            await _db.SaveChangesAsync(ct);
            return (await GetItemAsync(row.Id, ct))!;
        }

        public async Task<ItemDetailDto?> UpdateItemAsync(int id, ItemUpsertDto dto, CancellationToken ct = default)
        {
            var row = await _db.Items
                .Include(i => i.Specifications)
                .Include(i => i.TemplateAttributes)
                .FirstOrDefaultAsync(i => i.Id == id, ct);
            if (row == null) return null;

            if (row.ParentItemId.HasValue)
            {
                throw new InvalidOperationException("Use variant update for variant items.");
            }

            await EnsureItemCodeUniqueAsync(dto.ItemCode.Trim(), id, ct);
            row.ItemCode = dto.ItemCode.Trim();
            row.ItemName = dto.ItemName.Trim();
            row.ItemGroupId = dto.ItemGroupId;
            row.Description = dto.Description?.Trim() ?? string.Empty;
            row.Status = ItemMasterMappingHelper.ParseStatus(dto.Status);
            row.HasVariants = dto.HasVariants;
            row.UpdatedAt = DateTime.UtcNow;

            SyncSpecifications(row, dto.Specifications);
            SyncTemplateAttributes(row, dto.VariantAttributeIds);
            await _db.SaveChangesAsync(ct);
            return await GetItemAsync(id, ct);
        }

        public async Task<bool> DeleteItemAsync(int id, CancellationToken ct = default)
        {
            var row = await _db.Items
                .Include(i => i.Variants)
                .FirstOrDefaultAsync(i => i.Id == id, ct);
            if (row == null) return false;

            if (row.HasVariants && row.Variants.Count > 0)
            {
                throw new InvalidOperationException("Delete all variants before deleting a template item.");
            }

            _db.Items.Remove(row);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ItemDetailDto?> CreateVariantAsync(int parentId, ItemVariantUpsertDto dto, CancellationToken ct = default)
        {
            var parent = await _db.Items
                .Include(i => i.TemplateAttributes)
                .FirstOrDefaultAsync(i => i.Id == parentId, ct);
            if (parent == null || !parent.HasVariants)
            {
                throw new InvalidOperationException("Parent item not found or does not support variants.");
            }

            var itemCode = string.IsNullOrWhiteSpace(dto.ItemCode)
                ? await GenerateVariantCodeAsync(parent, dto.Attributes, ct)
                : dto.ItemCode.Trim();
            await EnsureItemCodeUniqueAsync(itemCode, null, ct);

            var now = DateTime.UtcNow;
            var variant = new Item
            {
                ItemCode = itemCode,
                ItemName = BuildVariantName(parent.ItemName, dto.Attributes),
                ItemGroupId = parent.ItemGroupId,
                Description = parent.Description,
                Status = ItemMasterMappingHelper.ParseStatus(dto.Status),
                HasVariants = false,
                ParentItemId = parentId,
                CreatedAt = now,
                UpdatedAt = now,
            };

            ApplyVariantAttributes(variant, dto.Attributes);
            ApplySpecifications(variant, dto.Specifications);
            _db.Items.Add(variant);
            await _db.SaveChangesAsync(ct);
            return await GetItemAsync(parentId, ct);
        }

        public async Task<ItemDetailDto?> GenerateVariantsAsync(int parentId, ItemVariantGenerateDto dto, CancellationToken ct = default)
        {
            var parent = await _db.Items
                .Include(i => i.TemplateAttributes)
                .FirstOrDefaultAsync(i => i.Id == parentId, ct);
            if (parent == null || !parent.HasVariants)
            {
                throw new InvalidOperationException("Parent item not found or does not support variants.");
            }

            if (dto.Attributes == null || dto.Attributes.Count == 0)
            {
                throw new InvalidOperationException("Provide at least one attribute with values to generate variants.");
            }

            var combinations = BuildCombinations(dto.Attributes);
            var existingSignatures = dto.SkipExisting
                ? await GetExistingVariantSignaturesAsync(parentId, ct)
                : new HashSet<string>();

            var now = DateTime.UtcNow;
            foreach (var combo in combinations)
            {
                var signature = BuildSignature(combo);
                if (existingSignatures.Contains(signature))
                {
                    continue;
                }

                var attrs = combo.Select(c => new ItemVariantAttributeUpsertDto
                {
                    AttributeId = c.AttributeId,
                    CustomValue = c.Value,
                }).ToList();

                var itemCode = await GenerateVariantCodeAsync(parent, attrs, ct);
                if (await _db.Items.AnyAsync(i => i.ItemCode == itemCode, ct))
                {
                    itemCode = $"{itemCode}-{Guid.NewGuid().ToString("N")[..4]}";
                }

                var variant = new Item
                {
                    ItemCode = itemCode,
                    ItemName = BuildVariantName(parent.ItemName, attrs),
                    ItemGroupId = parent.ItemGroupId,
                    Description = parent.Description,
                    Status = ItemMasterMappingHelper.ParseStatus(dto.Status),
                    HasVariants = false,
                    ParentItemId = parentId,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                ApplyVariantAttributes(variant, attrs);
                _db.Items.Add(variant);
            }

            await _db.SaveChangesAsync(ct);
            return await GetItemAsync(parentId, ct);
        }

        public async Task<bool> DeleteVariantAsync(int parentId, int variantId, CancellationToken ct = default)
        {
            var variant = await _db.Items.FirstOrDefaultAsync(
                i => i.Id == variantId && i.ParentItemId == parentId, ct);
            if (variant == null) return false;

            _db.Items.Remove(variant);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        private static IQueryable<Item> ApplyItemSort(IQueryable<Item> q, string? sortBy, string? sortDir)
        {
            var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            return (sortBy?.ToLowerInvariant()) switch
            {
                "itemcode" => desc ? q.OrderByDescending(i => i.ItemCode) : q.OrderBy(i => i.ItemCode),
                "createdat" => desc ? q.OrderByDescending(i => i.CreatedAt) : q.OrderBy(i => i.CreatedAt),
                "updatedat" => desc ? q.OrderByDescending(i => i.UpdatedAt) : q.OrderBy(i => i.UpdatedAt),
                "status" => desc ? q.OrderByDescending(i => i.Status) : q.OrderBy(i => i.Status),
                _ => desc ? q.OrderByDescending(i => i.ItemName) : q.OrderBy(i => i.ItemName),
            };
        }

        private async Task ValidateGroupParentAsync(int? parentId, int? selfId, CancellationToken ct)
        {
            if (!parentId.HasValue) return;

            if (selfId.HasValue && parentId.Value == selfId.Value)
            {
                throw new InvalidOperationException("A group cannot be its own parent.");
            }

            var parent = await _db.ItemGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == parentId.Value, ct);
            if (parent == null)
            {
                throw new InvalidOperationException("Parent group not found.");
            }

            if (selfId.HasValue)
            {
                var cursor = parent;
                while (cursor.ParentId.HasValue)
                {
                    if (cursor.ParentId.Value == selfId.Value)
                    {
                        throw new InvalidOperationException("Circular group hierarchy is not allowed.");
                    }

                    cursor = await _db.ItemGroups.AsNoTracking()
                        .FirstOrDefaultAsync(g => g.Id == cursor.ParentId.Value, ct)
                        ?? throw new InvalidOperationException("Invalid group hierarchy.");
                }
            }
        }

        private async Task EnsureAttributeCodeUniqueAsync(string code, int? excludeId, CancellationToken ct)
        {
            var exists = await _db.ItemAttributes.AnyAsync(
                a => a.Code == code && (!excludeId.HasValue || a.Id != excludeId.Value), ct);
            if (exists)
            {
                throw new InvalidOperationException($"Attribute code '{code}' already exists.");
            }
        }

        private async Task EnsureItemCodeUniqueAsync(string code, int? excludeId, CancellationToken ct)
        {
            var exists = await _db.Items.AnyAsync(
                i => i.ItemCode == code && (!excludeId.HasValue || i.Id != excludeId.Value), ct);
            if (exists)
            {
                throw new InvalidOperationException($"Item code '{code}' already exists.");
            }
        }

        private static void ApplyAttributeValues(ItemAttribute row, List<ItemAttributeValueUpsertDto> values)
        {
            var order = 0;
            foreach (var v in values.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
            {
                row.Values.Add(new ItemAttributeValue
                {
                    Value = v.Value.Trim(),
                    SortOrder = v.SortOrder > 0 ? v.SortOrder : order++,
                    IsActive = v.IsActive,
                });
            }
        }

        private static void SyncAttributeValues(ItemAttribute row, List<ItemAttributeValueUpsertDto> values)
        {
            var incomingIds = values.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();
            var toRemove = row.Values.Where(v => !incomingIds.Contains(v.Id)).ToList();
            foreach (var rem in toRemove)
            {
                row.Values.Remove(rem);
            }

            var order = 0;
            foreach (var v in values.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
            {
                if (v.Id.HasValue)
                {
                    var existing = row.Values.FirstOrDefault(x => x.Id == v.Id.Value);
                    if (existing != null)
                    {
                        existing.Value = v.Value.Trim();
                        existing.SortOrder = v.SortOrder > 0 ? v.SortOrder : order++;
                        existing.IsActive = v.IsActive;
                        continue;
                    }
                }

                row.Values.Add(new ItemAttributeValue
                {
                    Value = v.Value.Trim(),
                    SortOrder = v.SortOrder > 0 ? v.SortOrder : order++,
                    IsActive = v.IsActive,
                });
            }
        }

        private static void ApplySpecifications(Item row, List<ItemSpecificationUpsertDto> specs)
        {
            var order = 0;
            foreach (var s in specs.Where(x => !string.IsNullOrWhiteSpace(x.SpecName)))
            {
                row.Specifications.Add(new ItemSpecification
                {
                    SpecName = s.SpecName.Trim(),
                    SpecValue = s.SpecValue?.Trim() ?? string.Empty,
                    SortOrder = s.SortOrder > 0 ? s.SortOrder : order++,
                });
            }
        }

        private static void SyncSpecifications(Item row, List<ItemSpecificationUpsertDto> specs)
        {
            var incomingIds = specs.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();
            var toRemove = row.Specifications.Where(s => !incomingIds.Contains(s.Id)).ToList();
            foreach (var rem in toRemove)
            {
                row.Specifications.Remove(rem);
            }

            var order = 0;
            foreach (var s in specs.Where(x => !string.IsNullOrWhiteSpace(x.SpecName)))
            {
                if (s.Id.HasValue)
                {
                    var existing = row.Specifications.FirstOrDefault(x => x.Id == s.Id.Value);
                    if (existing != null)
                    {
                        existing.SpecName = s.SpecName.Trim();
                        existing.SpecValue = s.SpecValue?.Trim() ?? string.Empty;
                        existing.SortOrder = s.SortOrder > 0 ? s.SortOrder : order++;
                        continue;
                    }
                }

                row.Specifications.Add(new ItemSpecification
                {
                    SpecName = s.SpecName.Trim(),
                    SpecValue = s.SpecValue?.Trim() ?? string.Empty,
                    SortOrder = s.SortOrder > 0 ? s.SortOrder : order++,
                });
            }
        }

        private static void ApplyTemplateAttributes(Item row, List<int> attributeIds)
        {
            foreach (var attrId in attributeIds.Distinct())
            {
                row.TemplateAttributes.Add(new ItemTemplateAttribute { AttributeId = attrId });
            }
        }

        private static void SyncTemplateAttributes(Item row, List<int> attributeIds)
        {
            var incoming = attributeIds.Distinct().ToHashSet();
            var toRemove = row.TemplateAttributes.Where(t => !incoming.Contains(t.AttributeId)).ToList();
            foreach (var rem in toRemove)
            {
                row.TemplateAttributes.Remove(rem);
            }

            var existing = row.TemplateAttributes.Select(t => t.AttributeId).ToHashSet();
            foreach (var attrId in incoming.Where(id => !existing.Contains(id)))
            {
                row.TemplateAttributes.Add(new ItemTemplateAttribute { AttributeId = attrId });
            }
        }

        private void ApplyVariantAttributes(Item variant, List<ItemVariantAttributeUpsertDto> attrs)
        {
            foreach (var a in attrs)
            {
                variant.VariantAttributeValues.Add(new ItemVariantAttributeValue
                {
                    AttributeId = a.AttributeId,
                    AttributeValueId = a.AttributeValueId,
                    CustomValue = a.CustomValue?.Trim() ?? string.Empty,
                });
            }
        }

        private static string BuildVariantName(string parentName, List<ItemVariantAttributeUpsertDto> attrs)
        {
            var parts = attrs
                .Select(a => !string.IsNullOrWhiteSpace(a.CustomValue) ? a.CustomValue.Trim() : null)
                .Where(v => v != null)
                .ToList();
            return parts.Count == 0 ? parentName : $"{parentName} ({string.Join(", ", parts)})";
        }

        private async Task<string> GenerateVariantCodeAsync(
            Item parent,
            List<ItemVariantAttributeUpsertDto> attrs,
            CancellationToken ct)
        {
            var attrIds = attrs.Select(a => a.AttributeId).ToList();
            var attrCodes = await _db.ItemAttributes.AsNoTracking()
                .Where(a => attrIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Code, ct);

            var suffixParts = attrs.Select(a =>
            {
                var val = !string.IsNullOrWhiteSpace(a.CustomValue)
                    ? a.CustomValue
                    : a.AttributeValueId?.ToString() ?? "x";
                var code = attrCodes.TryGetValue(a.AttributeId, out var c) ? c : "attr";
                return ItemMasterMappingHelper.SlugifyCode($"{code}-{val}");
            });

            var suffix = string.Join("-", suffixParts.Where(s => !string.IsNullOrEmpty(s)));
            return string.IsNullOrEmpty(suffix) ? $"{parent.ItemCode}-var" : $"{parent.ItemCode}-{suffix}";
        }

        private static List<List<(int AttributeId, string Value)>> BuildCombinations(
            List<ItemVariantGenerateAttributeDto> attributes)
        {
            var lists = attributes
                .Where(a => a.Values != null && a.Values.Count > 0)
                .Select(a => a.Values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => (a.AttributeId, v.Trim()))
                    .ToList())
                .Where(l => l.Count > 0)
                .ToList();

            if (lists.Count == 0) return new List<List<(int, string)>>();

            IEnumerable<List<(int AttributeId, string Value)>> result = new[] { new List<(int, string)>() };
            foreach (var list in lists)
            {
                result = result.SelectMany(
                    acc => list,
                    (acc, item) =>
                    {
                        var next = new List<(int, string)>(acc) { item };
                        return next;
                    });
            }

            return result.ToList();
        }

        private async Task<HashSet<string>> GetExistingVariantSignaturesAsync(int parentId, CancellationToken ct)
        {
            var variants = await _db.Items.AsNoTracking()
                .Include(v => v.VariantAttributeValues)
                .Where(v => v.ParentItemId == parentId)
                .ToListAsync(ct);

            return variants.Select(v => BuildSignature(
                v.VariantAttributeValues.Select(a => (a.AttributeId, a.AttributeValue?.Value ?? a.CustomValue)).ToList()))
                .ToHashSet();
        }

        private static string BuildSignature(List<(int AttributeId, string Value)> attrs)
        {
            return string.Join("|", attrs
                .OrderBy(a => a.AttributeId)
                .Select(a => $"{a.AttributeId}:{a.Value.Trim().ToLowerInvariant()}"));
        }

        private static string BuildSignature(IEnumerable<(int AttributeId, string Value)> attrs)
        {
            return BuildSignature(attrs.ToList());
        }
    }
}
