using System.Text.Json;
using CRM.DTO;

namespace CRM.Helpers
{
    public static class QuotationGridColumnHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        public static List<QuotationGridColumnDto> ParseColumns(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return LoadDefaultColumns();
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<List<QuotationGridColumnDto>>(json, JsonOptions);
                return MergeWithDefaults(parsed ?? new List<QuotationGridColumnDto>());
            }
            catch
            {
                return LoadDefaultColumns();
            }
        }

        public static string SerializeColumns(IEnumerable<QuotationGridColumnDto> columns) =>
            JsonSerializer.Serialize(columns.OrderBy(c => c.Order).ToList(), JsonOptions);

        public static List<QuotationGridColumnDto> MergeWithDefaults(IEnumerable<QuotationGridColumnDto> saved)
        {
            var defaults = LoadDefaultColumns();
            var savedList = saved?.Where(c => !string.IsNullOrWhiteSpace(c.Key)).ToList()
                             ?? new List<QuotationGridColumnDto>();
            var map = savedList.ToDictionary(c => c.Key, StringComparer.OrdinalIgnoreCase);
            var merged = new List<QuotationGridColumnDto>();

            foreach (var def in defaults)
            {
                if (map.TryGetValue(def.Key, out var userCol))
                {
                    merged.Add(new QuotationGridColumnDto
                    {
                        Key = def.Key,
                        Label = string.IsNullOrWhiteSpace(userCol.Label) ? def.Label : userCol.Label.Trim(),
                        Visible = userCol.Visible,
                        Order = userCol.Order,
                        Width = userCol.Width > 0 ? userCol.Width : def.Width,
                        Editable = def.Editable,
                    });
                }
                else
                {
                    merged.Add(def);
                }
            }

            var mergedKeys = new HashSet<string>(merged.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);
            foreach (var col in savedList)
            {
                if (mergedKeys.Contains(col.Key) || !IsDynamicColumnKey(col.Key))
                {
                    continue;
                }

                merged.Add(new QuotationGridColumnDto
                {
                    Key = col.Key.Trim(),
                    Label = string.IsNullOrWhiteSpace(col.Label) ? col.Key.Trim() : col.Label.Trim(),
                    Visible = col.Visible,
                    Order = col.Order,
                    Width = col.Width > 0 ? col.Width : 110,
                    Editable = col.Editable,
                });
            }

            return merged.OrderBy(c => c.Order).Select((c, i) =>
            {
                c.Order = i;
                return c;
            }).ToList();
        }

        private static bool IsDynamicColumnKey(string key) =>
            key.StartsWith("attr:", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("spec:", StringComparison.OrdinalIgnoreCase);

        private static List<QuotationGridColumnDto> LoadDefaultColumns()
        {
            try
            {
                return JsonSerializer.Deserialize<List<QuotationGridColumnDto>>(
                           QuotationGridColumnDefaults.DefaultColumnsJson,
                           JsonOptions)
                       ?? new List<QuotationGridColumnDto>();
            }
            catch
            {
                return new List<QuotationGridColumnDto>();
            }
        }
    }
}
