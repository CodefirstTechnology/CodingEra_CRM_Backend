using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CRM.Services
{
    internal static class LeadSyncPullHelpers
    {
        public static List<JsonElement> ExtractLeadArray(JsonElement body)
        {
            if (body.ValueKind == JsonValueKind.Array)
            {
                return body.EnumerateArray().ToList();
            }

            if (body.ValueKind != JsonValueKind.Object)
            {
                return new List<JsonElement>();
            }

            foreach (var key in new[]
            {
                "RESPONSE", "response", "leads", "data", "DATA", "enquiries", "enquiry",
                "inquiries", "items", "result", "records", "rows",
            })
            {
                if (!body.TryGetProperty(key, out var value))
                {
                    continue;
                }

                if (value.ValueKind == JsonValueKind.Array)
                {
                    return value.EnumerateArray().ToList();
                }

                if (value.ValueKind == JsonValueKind.Object
                    && value.TryGetProperty("DATA", out var nested)
                    && nested.ValueKind == JsonValueKind.Array)
                {
                    return nested.EnumerateArray().ToList();
                }
            }

            return new List<JsonElement>();
        }

        public static string PickString(JsonElement row, params string[] names)
        {
            foreach (var name in names)
            {
                if (!row.TryGetProperty(name, out var value))
                {
                    continue;
                }

                var s = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString()?.Trim(),
                    JsonValueKind.Number => value.GetRawText(),
                    _ => null,
                };

                if (!string.IsNullOrWhiteSpace(s))
                {
                    return s!;
                }
            }

            return string.Empty;
        }

        public static LeadSyncIncomingLead? MapGenericMarketplaceRow(
            JsonElement row,
            string markerName,
            string defaultSourceLabel)
        {
            var customerName = PickString(row,
                "SENDERNAME", "sendername", "name", "customerName", "customer_name",
                "customer", "contact_name", "buyer_name", "sender_name", "lead_name", "caller_name");
            var mobile = PickString(row,
                "SENDERMOBILE", "MOBILE", "mobile", "phone", "Phone", "contact_number",
                "phone_number", "mobile_number", "sender_mobile", "caller_number");
            var email = PickString(row,
                "SENDEREMAIL", "email", "Email", "EMAIL", "buyer_email", "sender_email", "contact_email");
            var city = PickString(row,
                "SENDER_CITY", "city", "City", "location", "Location", "area", "locality", "customer_city");
            var requirement = PickString(row,
                "QUERY_MESSAGE", "SUBJECT", "requirement", "message", "Message", "query",
                "remarks", "comments", "description", "enquiry_text", "inquiry_text");
            var product = PickString(row,
                "PRODUCT_NAME", "product", "Product", "product_name", "service", "Service",
                "category", "requirement_for", "subject", "business_category");
            if (string.IsNullOrWhiteSpace(product) && !string.IsNullOrWhiteSpace(requirement))
            {
                product = requirement.Length > 120 ? requirement[..120] : requirement;
            }

            var extRef = PickString(row,
                "UNIQUE_QUERY_ID", "QUERY_ID", "enquiry_id", "inquiry_id", "externalRef",
                "external_ref", "id", "lead_id", "LeadId", "tradeindia_lead_id", "ti_lead_id",
                "jd_lead_id", "unique_id");

            if (string.IsNullOrWhiteSpace(customerName) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(mobile))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(extRef))
            {
                extRef = $"{email}|{mobile}".ToLowerInvariant();
            }

            var parts = (customerName.Length > 0 ? customerName : email.Length > 0 ? email : mobile)
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = parts.Length > 0 ? parts[0] : "Lead";
            var lastName = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : "Contact";

            var notesLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(requirement))
            {
                notesLines.Add(requirement);
            }

            if (!string.IsNullOrWhiteSpace(product))
            {
                notesLines.Add($"Product: {product}");
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                notesLines.Add($"City: {city}");
            }

            notesLines.Add($"[crm-ext:{markerName}:{extRef}]");

            return new LeadSyncIncomingLead
            {
                ExternalKey = extRef,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Mobile = mobile,
                Requirement = requirement,
                Notes = string.Join('\n', notesLines),
            };
        }

        public static string? TryGetIndiaMartError(JsonElement body)
        {
            if (body.ValueKind != JsonValueKind.Object)
            {
                return "Unexpected IndiaMART response.";
            }

            if (body.TryGetProperty("STATUS", out var status) && status.ValueKind == JsonValueKind.String)
            {
                var s = status.GetString()?.Trim();
                if (!string.Equals(s, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    if (body.TryGetProperty("MESSAGE", out var msg))
                    {
                        return msg.GetString() ?? s;
                    }

                    return s;
                }
            }

            return null;
        }

        public static (string start_time, string end_time) GetTodayIstPullTimeRange()
        {
            var parts = CultureInfo.InvariantCulture.DateTimeFormat;
            var ist = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            var day = now.ToString("dd", CultureInfo.InvariantCulture);
            var month = now.ToString("MMM", CultureInfo.InvariantCulture);
            var year = now.ToString("yyyy", CultureInfo.InvariantCulture);
            return (
                $"{day}-{month}-{year}00:00:00",
                $"{day}-{month}-{year}23:59:59");
        }

        public static string BuildIndiaMartPullUrl(LeadSyncResolvedCredentials credentials)
        {
            var baseUrl = credentials.PullApiUrl.Trim();
            var key = credentials.ApiKey.Trim();
            var url = baseUrl;
            if (!url.Contains("glusr_crm_key=", StringComparison.OrdinalIgnoreCase))
            {
                var separator = url.Contains('?') ? '&' : '?';
                url = $"{url}{separator}glusr_crm_key={Uri.EscapeDataString(key)}";
            }

            var (startTime, endTime) = GetTodayIstPullTimeRange();
            return AppendQueryParam(AppendQueryParam(url, "start_time", startTime), "end_time", endTime);
        }

        public static string BuildBearerPullUrl(LeadSyncResolvedCredentials credentials)
        {
            var url = credentials.PullApiUrl.Trim();
            if (url.Contains("api_key=", StringComparison.OrdinalIgnoreCase)
                || url.Contains("apikey=", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var separator = url.Contains('?') ? '&' : '?';
            return $"{url}{separator}api_key={Uri.EscapeDataString(credentials.ApiKey.Trim())}";
        }

        /// <summary>
        /// TradeIndia inquiry pull uses query params: userid, profile_id, key, from_date, to_date.
        /// userid/profile_id must already be present on the saved URL; key may come from URL or credentials.
        /// </summary>
        public static string BuildTradeIndiaPullUrl(LeadSyncResolvedCredentials credentials)
        {
            var url = credentials.PullApiUrl.Trim();
            var hasKey = url.Contains("key=", StringComparison.OrdinalIgnoreCase);
            if (!hasKey && !string.IsNullOrWhiteSpace(credentials.ApiKey))
            {
                url = AppendQueryParam(url, "key", credentials.ApiKey.Trim());
            }

            var (fromDate, toDate) = GetTodayIstDateRangeYmd();
            url = AppendQueryParam(url, "from_date", fromDate);
            url = AppendQueryParam(url, "to_date", toDate);
            return url;
        }

        public static (string fromDate, string toDate) GetTodayIstDateRangeYmd()
        {
            var ist = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            var day = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return (day, day);
        }

        public static bool TryParseJsonElement(string raw, out JsonElement body, out string? errorMessage)
        {
            body = default;
            errorMessage = null;
            var text = (raw ?? string.Empty).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            if (text.Length == 0)
            {
                errorMessage = "API returned an empty response.";
                return false;
            }

            if (text[0] is not ('{' or '['))
            {
                var preview = text.Length > 160 ? text[..160] + "…" : text;
                errorMessage =
                    "API did not return JSON. Check userid, profile_id, and key on the pull URL. "
                    + $"Response starts with: {preview}";
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(text);
                body = doc.RootElement.Clone();
                return true;
            }
            catch (JsonException ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static string FormatMarketplaceHttpError(string providerName, HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    $"{providerName} request was not authorized. Check your API key.",
                HttpStatusCode.Forbidden =>
                    $"{providerName} access was denied. Check your API key and permissions.",
                HttpStatusCode.NotFound =>
                    $"{providerName} sync endpoint was not found. Check the pull API URL.",
                HttpStatusCode.TooManyRequests =>
                    $"{providerName} rate limit reached. Wait a few minutes before syncing again.",
                HttpStatusCode.RequestTimeout =>
                    $"{providerName} request timed out. Try again shortly.",
                _ when (int)statusCode >= 500 =>
                    $"{providerName} service returned a server error. Try again later.",
                _ => $"{providerName} API returned HTTP {(int)statusCode}.",
            };
        }

        private static string AppendQueryParam(string url, string name, string value)
        {
            if (url.Contains($"{name}=", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var separator = url.Contains('?') ? '&' : '?';
            return $"{url}{separator}{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
        }
    }
}
