using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRM.DTO
{
    /// <summary>
    /// Inbound IndiaMART Webhook / Push API payload DTO.
    /// Uses a tolerant JsonConverter to support UPPERCASE, snake_case, and PascalCase aliases
    /// without property name collisions in ASP.NET Core.
    /// </summary>
    [JsonConverter(typeof(IndiaMartWebhookLeadDtoConverter))]
    public class IndiaMartWebhookLeadDto
    {
        public string? UniqueQueryId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderMobile { get; set; }
        public string? SenderMobileAlt { get; set; }
        public string? SenderEmail { get; set; }
        public string? Subject { get; set; }
        public string? QueryProductName { get; set; }
        public string? QueryMessage { get; set; }
        public string? GlusrUsrCompanyName { get; set; }
        public string? SenderCity { get; set; }
        public string? SenderState { get; set; }
        public string? SenderPincode { get; set; }
        public string? QueryTime { get; set; }
        public string? GlusrCrmKey { get; set; }

        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        public string? GetEffectiveSenderName()
        {
            return SenderName?.Trim();
        }

        public string? GetEffectiveSenderMobile()
        {
            if (!string.IsNullOrWhiteSpace(SenderMobile)) return SenderMobile.Trim();
            return SenderMobileAlt?.Trim();
        }

        public string? GetEffectiveCompanyName()
        {
            return GlusrUsrCompanyName?.Trim();
        }

        public string? GetEffectiveProductName()
        {
            if (!string.IsNullOrWhiteSpace(QueryProductName)) return QueryProductName.Trim();
            return Subject?.Trim();
        }

        public string? GetEffectiveMessage()
        {
            return QueryMessage?.Trim();
        }

        public string GetEffectiveExternalKey()
        {
            if (!string.IsNullOrWhiteSpace(UniqueQueryId))
            {
                return UniqueQueryId.Trim();
            }

            var mobile = GetEffectiveSenderMobile();
            var email = SenderEmail?.Trim();
            return $"{email}|{mobile}".ToLowerInvariant();
        }
    }

    public sealed class IndiaMartWebhookLeadDtoConverter : JsonConverter<IndiaMartWebhookLeadDto>
    {
        public override IndiaMartWebhookLeadDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var dto = new IndiaMartWebhookLeadDto();
            var extra = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in root.EnumerateObject())
            {
                var name = prop.Name;
                var val = prop.Value;
                var strVal = val.ValueKind switch
                {
                    JsonValueKind.String => val.GetString()?.Trim(),
                    JsonValueKind.Number => val.GetRawText().Trim(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => null
                };

                switch (name.ToLowerInvariant())
                {
                    case "unique_query_id":
                    case "queryid":
                    case "query_id":
                    case "leadid":
                    case "lead_id":
                    case "id":
                        dto.UniqueQueryId ??= strVal;
                        break;

                    case "sender_name":
                    case "name":
                    case "customername":
                    case "buyer_name":
                        dto.SenderName ??= strVal;
                        break;

                    case "sender_mobile":
                    case "mobile":
                    case "phone":
                        dto.SenderMobile ??= strVal;
                        break;

                    case "sender_mobile_alt":
                    case "mobile_alt":
                        dto.SenderMobileAlt ??= strVal;
                        break;

                    case "sender_email":
                    case "email":
                        dto.SenderEmail ??= strVal;
                        break;

                    case "subject":
                        dto.Subject ??= strVal;
                        break;

                    case "query_product_name":
                    case "product":
                    case "product_name":
                    case "query_mcat_name":
                        dto.QueryProductName ??= strVal;
                        break;

                    case "query_message":
                    case "message":
                    case "requirement":
                        dto.QueryMessage ??= strVal;
                        break;

                    case "glusr_usr_companyname":
                    case "sender_company":
                    case "company":
                    case "company_name":
                        dto.GlusrUsrCompanyName ??= strVal;
                        break;

                    case "sender_city":
                    case "city":
                        dto.SenderCity ??= strVal;
                        break;

                    case "sender_state":
                    case "state":
                        dto.SenderState ??= strVal;
                        break;

                    case "sender_pincode":
                    case "pincode":
                        dto.SenderPincode ??= strVal;
                        break;

                    case "query_time":
                    case "timestamp":
                    case "date":
                        dto.QueryTime ??= strVal;
                        break;

                    case "glusr_crm_key":
                    case "apikey":
                    case "api_key":
                        dto.GlusrCrmKey ??= strVal;
                        break;

                    default:
                        extra[name] = val.Clone();
                        break;
                }
            }

            if (extra.Count > 0)
            {
                dto.ExtensionData = extra;
            }

            return dto;
        }

        public override void Write(Utf8JsonWriter writer, IndiaMartWebhookLeadDto value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.UniqueQueryId != null) writer.WriteString("UNIQUE_QUERY_ID", value.UniqueQueryId);
            if (value.SenderName != null) writer.WriteString("SENDER_NAME", value.SenderName);
            if (value.SenderMobile != null) writer.WriteString("SENDER_MOBILE", value.SenderMobile);
            if (value.SenderMobileAlt != null) writer.WriteString("SENDER_MOBILE_ALT", value.SenderMobileAlt);
            if (value.SenderEmail != null) writer.WriteString("SENDER_EMAIL", value.SenderEmail);
            if (value.Subject != null) writer.WriteString("SUBJECT", value.Subject);
            if (value.QueryProductName != null) writer.WriteString("QUERY_PRODUCT_NAME", value.QueryProductName);
            if (value.QueryMessage != null) writer.WriteString("QUERY_MESSAGE", value.QueryMessage);
            if (value.GlusrUsrCompanyName != null) writer.WriteString("GLUSR_USR_COMPANYNAME", value.GlusrUsrCompanyName);
            if (value.SenderCity != null) writer.WriteString("SENDER_CITY", value.SenderCity);
            if (value.SenderState != null) writer.WriteString("SENDER_STATE", value.SenderState);
            if (value.SenderPincode != null) writer.WriteString("SENDER_PINCODE", value.SenderPincode);
            if (value.QueryTime != null) writer.WriteString("QUERY_TIME", value.QueryTime);
            if (value.GlusrCrmKey != null) writer.WriteString("GLUSR_CRM_KEY", value.GlusrCrmKey);

            if (value.ExtensionData != null)
            {
                foreach (var kvp in value.ExtensionData)
                {
                    writer.WritePropertyName(kvp.Key);
                    kvp.Value.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }
    }
}
