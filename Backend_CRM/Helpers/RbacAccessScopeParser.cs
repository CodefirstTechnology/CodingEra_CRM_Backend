using CRM.models;
using System.Text.Json;

namespace CRM.Helpers
{
    public static class RbacAccessScopeParser
    {
        public static AccessScope Parse(object? value, AccessScope fallback = AccessScope.Own)
        {
            if (value == null)
            {
                return fallback;
            }

            if (value is AccessScope scope)
            {
                return scope;
            }

            if (value is JsonElement el)
            {
                return ParseJsonElement(el, fallback);
            }

            if (value is int i)
            {
                return ToScope(i, fallback);
            }

            if (value is long l)
            {
                return ToScope((int)l, fallback);
            }

            if (value is string s)
            {
                return ParseString(s, fallback);
            }

            if (int.TryParse(value.ToString(), out var n))
            {
                return ToScope(n, fallback);
            }

            return fallback;
        }

        private static AccessScope ParseJsonElement(JsonElement el, AccessScope fallback)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => ToScope(el.GetInt32(), fallback),
                JsonValueKind.String => ParseString(el.GetString(), fallback),
                _ => fallback,
            };
        }

        private static AccessScope ParseString(string? s, AccessScope fallback)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return fallback;
            }

            var v = s.Trim().ToLowerInvariant();
            return v switch
            {
                "all" or "2" => AccessScope.All,
                "team" or "1" => AccessScope.Team,
                "own" or "0" => AccessScope.Own,
                _ => int.TryParse(v, out var n) ? ToScope(n, fallback) : fallback,
            };
        }

        private static AccessScope ToScope(int value, AccessScope fallback)
        {
            return value switch
            {
                2 => AccessScope.All,
                1 => AccessScope.Team,
                0 => AccessScope.Own,
                _ => fallback,
            };
        }
    }
}
