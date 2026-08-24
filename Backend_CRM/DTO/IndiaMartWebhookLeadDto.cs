using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRM.DTO
{
    /// <summary>
    /// Inbound IndiaMART Webhook / Push API payload DTO.
    /// Supports both wrapped envelope shape ({ "CODE": 200, "STATUS": "SUCCESS", "RESPONSE": { ... } })
    /// and flat/direct shape ({ "UNIQUE_QUERY_ID": "...", "SENDER_NAME": "...", ... }).
    /// Tolerantly handles numeric/string types, empty strings, and arbitrary casing.
    /// </summary>
    [JsonConverter(typeof(IndiaMartWebhookLeadDtoConverter))]
    public class IndiaMartWebhookLeadDto
    {
        // Envelope headers
        public int? Code { get; set; }
        public string? Status { get; set; }
        public string? StatusMessage { get; set; }

        // Core lead fields
        public string? UniqueQueryId { get; set; }
        public string? Subject { get; set; }
        public string? QueryTime { get; set; }
        public string? QueryType { get; set; }
        public string? SenderName { get; set; }
        public string? SenderMobile { get; set; }
        public string? SenderMobileAlt { get; set; }
        public string? SenderPhone { get; set; }
        public string? SenderPhoneAlt { get; set; }
        public string? SenderEmail { get; set; }
        public string? SenderEmailAlt { get; set; }
        public string? SenderCompany { get; set; }
        public string? GlusrUsrCompanyName { get; set; }
        public string? SenderAddress { get; set; }
        public string? SenderCity { get; set; }
        public string? SenderState { get; set; }
        public string? SenderPincode { get; set; }
        public string? SenderCountryIso { get; set; }
        public string? QueryProductName { get; set; }
        public string? QueryMcatName { get; set; }
        public string? QueryMessage { get; set; }
        public string? CallDuration { get; set; }
        public string? ReceiverMobile { get; set; }
        public string? GlusrCrmKey { get; set; }

        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        public string? GetEffectiveSenderName()
        {
            if (!string.IsNullOrWhiteSpace(SenderName)) return SenderName.Trim();
            return GetExtensionString("name", "Name", "CustomerName", "customer_name", "buyer_name");
        }

        public string? GetEffectiveSenderMobile()
        {
            if (!string.IsNullOrWhiteSpace(SenderMobile)) return SenderMobile.Trim();
            if (!string.IsNullOrWhiteSpace(SenderPhone)) return SenderPhone.Trim();
            if (!string.IsNullOrWhiteSpace(SenderMobileAlt)) return SenderMobileAlt.Trim();
            if (!string.IsNullOrWhiteSpace(SenderPhoneAlt)) return SenderPhoneAlt.Trim();
            return GetExtensionString("mobile", "Mobile", "phone", "Phone", "contact_number");
        }

        public string? GetEffectiveCompanyName()
        {
            if (!string.IsNullOrWhiteSpace(SenderCompany)) return SenderCompany.Trim();
            if (!string.IsNullOrWhiteSpace(GlusrUsrCompanyName)) return GlusrUsrCompanyName.Trim();
            return GetExtensionString("company", "Company", "company_name");
        }

        public string? GetEffectiveProductName()
        {
            if (!string.IsNullOrWhiteSpace(QueryProductName)) return QueryProductName.Trim();
            if (!string.IsNullOrWhiteSpace(Subject)) return Subject.Trim();
            if (!string.IsNullOrWhiteSpace(QueryMcatName)) return QueryMcatName.Trim();
            return GetExtensionString("product", "Product", "subject", "Subject", "query_mcat_name");
        }

        public string? GetEffectiveMessage()
        {
            if (!string.IsNullOrWhiteSpace(QueryMessage)) return QueryMessage.Trim();
            if (!string.IsNullOrWhiteSpace(Subject)) return Subject.Trim();
            return GetExtensionString("message", "Message", "requirement", "Requirement");
        }

        public string GetEffectiveExternalKey()
        {
            if (!string.IsNullOrWhiteSpace(UniqueQueryId))
            {
                return UniqueQueryId.Trim();
            }

            var ext = GetExtensionString("UNIQUE_QUERY_ID", "unique_query_id", "query_id", "lead_id", "id", "enquiry_id");
            if (!string.IsNullOrEmpty(ext)) return ext;

            var mobile = GetEffectiveSenderMobile();
            var email = SenderEmail?.Trim();
            return $"{email}|{mobile}".ToLowerInvariant();
        }

        private string? GetExtensionString(params string[] keys)
        {
            if (ExtensionData == null) return null;
            foreach (var key in keys)
            {
                if (ExtensionData.TryGetValue(key, out var val))
                {
                    if (val.ValueKind == JsonValueKind.String)
                    {
                        var s = val.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(s)) return s;
                    }
                    else if (val.ValueKind == JsonValueKind.Number)
                    {
                        return val.GetRawText().Trim();
                    }
                }
            }
            return null;
        }
    }

    public sealed class IndiaMartWebhookLeadDtoConverter : JsonConverter<IndiaMartWebhookLeadDto>
    {
        public override IndiaMartWebhookLeadDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for IndiaMart webhook payload");
            }

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var dto = new IndiaMartWebhookLeadDto();
            var extra = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            // 1. Check if the payload is wrapped in an envelope (e.g. { "CODE": 200, "STATUS": "SUCCESS", "RESPONSE": { ... } })
            JsonElement? leadElement = null;

            foreach (var prop in root.EnumerateObject())
            {
                var name = prop.Name;
                var val = prop.Value;
                var strVal = ToStringValue(val);

                switch (name.ToLowerInvariant())
                {
                    case "code":
                        if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var codeInt))
                        {
                            dto.Code = codeInt;
                        }
                        else if (int.TryParse(strVal, out var parsedCode))
                        {
                            dto.Code = parsedCode;
                        }
                        break;

                    case "status":
                        dto.Status = strVal;
                        break;

                    case "status_message":
                    case "message":
                    case "msg":
                        dto.StatusMessage = strVal;
                        break;

                    case "response":
                    case "data":
                    case "lead":
                    case "leads":
                    case "enquiry":
                    case "inquiry":
                        if (val.ValueKind == JsonValueKind.Object)
                        {
                            leadElement = val;
                        }
                        else if (val.ValueKind == JsonValueKind.Array && val.GetArrayLength() > 0)
                        {
                            leadElement = val[0];
                        }
                        break;

                    default:
                        // Also populate directly in case of a flat/unwrapped payload
                        PopulateField(dto, name, val, strVal, extra);
                        break;
                }
            }

            // 2. If an inner RESPONSE object/array was found, populate from it
            if (leadElement.HasValue && leadElement.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in leadElement.Value.EnumerateObject())
                {
                    var name = prop.Name;
                    var val = prop.Value;
                    var strVal = ToStringValue(val);
                    PopulateField(dto, name, val, strVal, extra);
                }
            }

            if (extra.Count > 0)
            {
                dto.ExtensionData = extra;
            }

            return dto;
        }

        private static void PopulateField(
            IndiaMartWebhookLeadDto dto,
            string name,
            JsonElement val,
            string? strVal,
            Dictionary<string, JsonElement> extra)
        {
            switch (name.ToLowerInvariant())
            {
                case "unique_query_id":
                case "queryid":
                case "query_id":
                case "leadid":
                case "lead_id":
                case "id":
                case "enquiry_id":
                    dto.UniqueQueryId ??= strVal;
                    break;

                case "subject":
                    dto.Subject ??= strVal;
                    break;

                case "query_time":
                case "timestamp":
                case "date":
                    dto.QueryTime ??= strVal;
                    break;

                case "query_type":
                    dto.QueryType ??= strVal;
                    break;

                case "sender_name":
                case "name":
                case "customername":
                case "customer_name":
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

                case "sender_phone":
                    dto.SenderPhone ??= strVal;
                    break;

                case "sender_phone_alt":
                case "phone_alt":
                    dto.SenderPhoneAlt ??= strVal;
                    break;

                case "sender_email":
                case "email":
                    dto.SenderEmail ??= strVal;
                    break;

                case "sender_email_alt":
                case "email_alt":
                    dto.SenderEmailAlt ??= strVal;
                    break;

                case "sender_company":
                case "company":
                case "company_name":
                    dto.SenderCompany ??= strVal;
                    dto.GlusrUsrCompanyName ??= strVal;
                    break;

                case "glusr_usr_companyname":
                    dto.GlusrUsrCompanyName ??= strVal;
                    dto.SenderCompany ??= strVal;
                    break;

                case "sender_address":
                case "address":
                    dto.SenderAddress ??= strVal;
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
                case "pin":
                    dto.SenderPincode ??= strVal;
                    break;

                case "sender_country_iso":
                case "country":
                    dto.SenderCountryIso ??= strVal;
                    break;

                case "query_product_name":
                case "product":
                case "product_name":
                    dto.QueryProductName ??= strVal;
                    break;

                case "query_mcat_name":
                case "mcat_name":
                case "mcat":
                    dto.QueryMcatName ??= strVal;
                    break;

                case "query_message":
                case "message":
                case "requirement":
                    dto.QueryMessage ??= strVal;
                    break;

                case "call_duration":
                    dto.CallDuration ??= strVal;
                    break;

                case "receiver_mobile":
                    dto.ReceiverMobile ??= strVal;
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

        private static string? ToStringValue(JsonElement val)
        {
            return val.ValueKind switch
            {
                JsonValueKind.String => val.GetString()?.Trim(),
                JsonValueKind.Number => val.GetRawText().Trim(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, IndiaMartWebhookLeadDto value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.Code.HasValue) writer.WriteNumber("CODE", value.Code.Value);
            if (value.Status != null) writer.WriteString("STATUS", value.Status);

            writer.WriteStartObject("RESPONSE");
            if (value.UniqueQueryId != null) writer.WriteString("UNIQUE_QUERY_ID", value.UniqueQueryId);
            if (value.Subject != null) writer.WriteString("SUBJECT", value.Subject);
            if (value.QueryTime != null) writer.WriteString("QUERY_TIME", value.QueryTime);
            if (value.QueryType != null) writer.WriteString("QUERY_TYPE", value.QueryType);
            if (value.SenderName != null) writer.WriteString("SENDER_NAME", value.SenderName);
            if (value.SenderMobile != null) writer.WriteString("SENDER_MOBILE", value.SenderMobile);
            if (value.SenderMobileAlt != null) writer.WriteString("SENDER_MOBILE_ALT", value.SenderMobileAlt);
            if (value.SenderPhone != null) writer.WriteString("SENDER_PHONE", value.SenderPhone);
            if (value.SenderPhoneAlt != null) writer.WriteString("SENDER_PHONE_ALT", value.SenderPhoneAlt);
            if (value.SenderEmail != null) writer.WriteString("SENDER_EMAIL", value.SenderEmail);
            if (value.SenderEmailAlt != null) writer.WriteString("SENDER_EMAIL_ALT", value.SenderEmailAlt);
            if (value.SenderCompany != null) writer.WriteString("SENDER_COMPANY", value.SenderCompany);
            if (value.GlusrUsrCompanyName != null) writer.WriteString("GLUSR_USR_COMPANYNAME", value.GlusrUsrCompanyName);
            if (value.SenderAddress != null) writer.WriteString("SENDER_ADDRESS", value.SenderAddress);
            if (value.SenderCity != null) writer.WriteString("SENDER_CITY", value.SenderCity);
            if (value.SenderState != null) writer.WriteString("SENDER_STATE", value.SenderState);
            if (value.SenderPincode != null) writer.WriteString("SENDER_PINCODE", value.SenderPincode);
            if (value.SenderCountryIso != null) writer.WriteString("SENDER_COUNTRY_ISO", value.SenderCountryIso);
            if (value.QueryProductName != null) writer.WriteString("QUERY_PRODUCT_NAME", value.QueryProductName);
            if (value.QueryMcatName != null) writer.WriteString("QUERY_MCAT_NAME", value.QueryMcatName);
            if (value.QueryMessage != null) writer.WriteString("QUERY_MESSAGE", value.QueryMessage);
            if (value.CallDuration != null) writer.WriteString("CALL_DURATION", value.CallDuration);
            if (value.ReceiverMobile != null) writer.WriteString("RECEIVER_MOBILE", value.ReceiverMobile);
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

            writer.WriteEndObject();
        }
    }
}
