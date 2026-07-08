using CRM.DTO;
using CRM.models;

namespace CRM.Helpers
{
    public static class ItemMasterMappingHelper
    {
        public static ItemGroupDto ToGroupDto(ItemGroup g, int itemCount = 0, int childCount = 0, string? parentName = null)
        {
            return new ItemGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                ParentId = g.ParentId,
                ParentName = parentName,
                Description = g.Description,
                SortOrder = g.SortOrder,
                IsActive = g.IsActive,
                ItemCount = itemCount,
                ChildCount = childCount,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt,
            };
        }

        public static ItemAttributeDto ToAttributeDto(ItemAttribute a)
        {
            return new ItemAttributeDto
            {
                Id = a.Id,
                Name = a.Name,
                Code = a.Code,
                ValueType = a.ValueType.ToString(),
                IsVariantAttribute = a.IsVariantAttribute,
                SortOrder = a.SortOrder,
                IsActive = a.IsActive,
                Values = a.Values
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.Value)
                    .Select(ToAttributeValueDto)
                    .ToList(),
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
            };
        }

        public static ItemAttributeValueDto ToAttributeValueDto(ItemAttributeValue v)
        {
            return new ItemAttributeValueDto
            {
                Id = v.Id,
                AttributeId = v.AttributeId,
                Value = v.Value,
                SortOrder = v.SortOrder,
                IsActive = v.IsActive,
            };
        }

        public static ItemSpecificationDto ToSpecificationDto(ItemSpecification s)
        {
            return new ItemSpecificationDto
            {
                Id = s.Id,
                SpecName = s.SpecName,
                SpecValue = s.SpecValue,
                SortOrder = s.SortOrder,
            };
        }

        public static ItemVariantAttributeDto ToVariantAttributeDto(ItemVariantAttributeValue v)
        {
            var value = v.AttributeValue?.Value ?? v.CustomValue;
            return new ItemVariantAttributeDto
            {
                AttributeId = v.AttributeId,
                AttributeName = v.Attribute?.Name ?? string.Empty,
                AttributeCode = v.Attribute?.Code ?? string.Empty,
                AttributeValueId = v.AttributeValueId,
                Value = value,
            };
        }

        public static ItemListItemDto ToListItemDto(
            Item item,
            string? groupName = null,
            string? parentName = null,
            int variantCount = 0,
            IEnumerable<ItemVariantAttributeValue>? variantAttrs = null)
        {
            return new ItemListItemDto
            {
                Id = item.Id,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                ItemGroupId = item.ItemGroupId,
                ItemGroupName = groupName ?? item.ItemGroup?.Name ?? string.Empty,
                Status = item.Status.ToString(),
                HasVariants = item.HasVariants,
                ParentItemId = item.ParentItemId,
                ParentItemName = parentName ?? item.ParentItem?.ItemName ?? string.Empty,
                VariantCount = variantCount,
                VariantAttributes = variantAttrs?.Select(ToVariantAttributeDto).ToList() ?? new List<ItemVariantAttributeDto>(),
                SteelRate = item.SteelRate < 0 ? 0 : item.SteelRate,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
            };
        }

        public const int ItemCodeMaxLength = 64;

        public static decimal NormalizeSteelRate(decimal rate) => rate < 0 ? 0 : rate;

        /// <summary>Truncates generated variant codes to fit <see cref="ItemCodeMaxLength"/> with a stable short hash suffix.</summary>
        public static string FitItemCode(string code, string uniquenessSeed)
        {
            if (string.IsNullOrEmpty(code) || code.Length <= ItemCodeMaxLength)
            {
                return code;
            }

            var hash = ShortHash(uniquenessSeed);
            var suffix = $"-{hash}";
            var maxBase = ItemCodeMaxLength - suffix.Length;
            if (maxBase < 1)
            {
                return hash[..Math.Min(ItemCodeMaxLength, hash.Length)];
            }

            var basePart = code[..maxBase].TrimEnd('-');
            return $"{basePart}{suffix}";
        }

        public static string ShortHash(string input)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes)[..6].ToLowerInvariant();
        }

        public static ItemStatus ParseStatus(string? status)
        {
            return Enum.TryParse<ItemStatus>(status, true, out var parsed)
                ? parsed
                : ItemStatus.Active;
        }

        public static ItemAttributeValueType ParseValueType(string? valueType)
        {
            return Enum.TryParse<ItemAttributeValueType>(valueType, true, out var parsed)
                ? parsed
                : ItemAttributeValueType.Text;
        }

        public static string SlugifyCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var chars = input.Trim().ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray();
            var slug = new string(chars);
            while (slug.Contains("--", StringComparison.Ordinal))
            {
                slug = slug.Replace("--", "-", StringComparison.Ordinal);
            }

            return slug.Trim('-');
        }
    }
}
