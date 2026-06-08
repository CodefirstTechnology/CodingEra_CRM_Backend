using System.Text.Json;
using CRM.DTO;
using CRM.models;

namespace CRM.Helpers
{
    public class QuotationItemSnapshot
    {
        public List<QuotationSnapshotFieldDto> Attributes { get; set; } = new();
        public List<QuotationSnapshotFieldDto> Specifications { get; set; } = new();
        public decimal UnitWeight { get; set; }
    }

    public class QuotationSnapshotFieldDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public static class QuotationItemSnapshotHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        public static string Serialize(QuotationItemSnapshot snapshot) =>
            JsonSerializer.Serialize(snapshot, JsonOptions);

        public static QuotationItemSnapshot Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new QuotationItemSnapshot();
            }

            try
            {
                return JsonSerializer.Deserialize<QuotationItemSnapshot>(json, JsonOptions) ?? new QuotationItemSnapshot();
            }
            catch
            {
                return new QuotationItemSnapshot();
            }
        }

        public static QuotationItemSnapshot BuildFromEntity(Item item)
        {
            var snapshot = new QuotationItemSnapshot
            {
                Attributes = item.VariantAttributeValues
                    .Select(v => new QuotationSnapshotFieldDto
                    {
                        Key = NormalizeKey(v.Attribute?.Code, v.Attribute?.Name),
                        Label = v.Attribute?.Name ?? string.Empty,
                        Value = v.AttributeValue?.Value ?? v.CustomValue ?? string.Empty,
                    })
                    .Where(a => !string.IsNullOrWhiteSpace(a.Label) || !string.IsNullOrWhiteSpace(a.Key))
                    .ToList(),
                Specifications = item.Specifications
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new QuotationSnapshotFieldDto
                    {
                        Key = NormalizeKey(s.SpecName, s.SpecName),
                        Label = s.SpecName.Trim(),
                        Value = s.SpecValue ?? string.Empty,
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s.Label))
                    .ToList(),
            };
            snapshot.UnitWeight = ResolveUnitWeight(snapshot);
            return snapshot;
        }

        public static QuotationItemSnapshot BuildFromItem(ItemDetailDto item)
        {
            var snapshot = new QuotationItemSnapshot
            {
                Attributes = item.VariantAttributes
                    .Where(a => !string.IsNullOrWhiteSpace(a.AttributeCode) || !string.IsNullOrWhiteSpace(a.AttributeName))
                    .Select(a => new QuotationSnapshotFieldDto
                    {
                        Key = NormalizeKey(a.AttributeCode, a.AttributeName),
                        Label = string.IsNullOrWhiteSpace(a.AttributeName) ? a.AttributeCode : a.AttributeName,
                        Value = a.Value ?? string.Empty,
                    })
                    .ToList(),
                Specifications = item.Specifications
                    .Where(s => !string.IsNullOrWhiteSpace(s.SpecName))
                    .Select(s => new QuotationSnapshotFieldDto
                    {
                        Key = NormalizeKey(s.SpecName, s.SpecName),
                        Label = s.SpecName.Trim(),
                        Value = s.SpecValue ?? string.Empty,
                    })
                    .ToList(),
            };

            snapshot.UnitWeight = ResolveUnitWeight(snapshot);
            return snapshot;
        }

        public static decimal ResolveUnitWeight(QuotationItemSnapshot snapshot)
        {
            if (snapshot.UnitWeight > 0)
            {
                return snapshot.UnitWeight;
            }

            var fromSpec = snapshot.Specifications
                .FirstOrDefault(s => IsWeightKey(s.Key) || IsWeightKey(s.Label));
            if (fromSpec != null && decimal.TryParse(fromSpec.Value, out var specWeight) && specWeight > 0)
            {
                return specWeight;
            }

            var fromAttr = snapshot.Attributes
                .FirstOrDefault(s => IsWeightKey(s.Key) || IsWeightKey(s.Label));
            if (fromAttr != null && decimal.TryParse(fromAttr.Value, out var attrWeight) && attrWeight > 0)
            {
                return attrWeight;
            }

            return 0;
        }

        public static string NormalizeKey(string? code, string? name)
        {
            var raw = !string.IsNullOrWhiteSpace(code) ? code : name ?? string.Empty;
            return ItemMasterMappingHelper.SlugifyCode(raw);
        }

        private static bool IsWeightKey(string value) =>
            string.Equals(value?.Trim(), "weight", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ItemMasterMappingHelper.SlugifyCode(value), "weight", StringComparison.OrdinalIgnoreCase);
    }
}
