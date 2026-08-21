using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRM.DTO
{
    /// <summary>
    /// Inbound IndiaMART Webhook / Push API payload DTO.
    /// Supports standard IndiaMART field naming (UPPERCASE, snake_case, PascalCase)
    /// and tolerant deserialization via JsonExtensionData.
    /// </summary>
    public class IndiaMartWebhookLeadDto
    {
        [JsonPropertyName("UNIQUE_QUERY_ID")]
        public string? UniqueQueryId { get; set; }

        [JsonPropertyName("unique_query_id")]
        public string? UniqueQueryIdLower { set => UniqueQueryId ??= value; }

        [JsonPropertyName("QueryId")]
        public string? QueryId { set => UniqueQueryId ??= value; }

        [JsonPropertyName("query_id")]
        public string? QueryIdLower { set => UniqueQueryId ??= value; }

        [JsonPropertyName("LeadId")]
        public string? LeadId { set => UniqueQueryId ??= value; }

        [JsonPropertyName("lead_id")]
        public string? LeadIdLower { set => UniqueQueryId ??= value; }

        [JsonPropertyName("SENDER_NAME")]
        public string? SenderName { get; set; }

        [JsonPropertyName("sender_name")]
        public string? SenderNameLower { set => SenderName ??= value; }

        [JsonPropertyName("Name")]
        public string? Name { set => SenderName ??= value; }

        [JsonPropertyName("name")]
        public string? NameLower { set => SenderName ??= value; }

        [JsonPropertyName("CustomerName")]
        public string? CustomerName { set => SenderName ??= value; }

        [JsonPropertyName("buyer_name")]
        public string? BuyerName { set => SenderName ??= value; }

        [JsonPropertyName("SENDER_MOBILE")]
        public string? SenderMobile { get; set; }

        [JsonPropertyName("sender_mobile")]
        public string? SenderMobileLower { set => SenderMobile ??= value; }

        [JsonPropertyName("Mobile")]
        public string? Mobile { set => SenderMobile ??= value; }

        [JsonPropertyName("mobile")]
        public string? MobileLower { set => SenderMobile ??= value; }

        [JsonPropertyName("Phone")]
        public string? Phone { set => SenderMobile ??= value; }

        [JsonPropertyName("phone")]
        public string? PhoneLower { set => SenderMobile ??= value; }

        [JsonPropertyName("SENDER_MOBILE_ALT")]
        public string? SenderMobileAlt { get; set; }

        [JsonPropertyName("SENDER_EMAIL")]
        public string? SenderEmail { get; set; }

        [JsonPropertyName("sender_email")]
        public string? SenderEmailLower { set => SenderEmail ??= value; }

        [JsonPropertyName("Email")]
        public string? Email { set => SenderEmail ??= value; }

        [JsonPropertyName("email")]
        public string? EmailLower { set => SenderEmail ??= value; }

        [JsonPropertyName("SUBJECT")]
        public string? Subject { get; set; }

        [JsonPropertyName("subject")]
        public string? SubjectLower { set => Subject ??= value; }

        [JsonPropertyName("QUERY_PRODUCT_NAME")]
        public string? QueryProductName { get; set; }

        [JsonPropertyName("query_product_name")]
        public string? QueryProductNameLower { set => QueryProductName ??= value; }

        [JsonPropertyName("Product")]
        public string? Product { set => QueryProductName ??= value; }

        [JsonPropertyName("product")]
        public string? ProductLower { set => QueryProductName ??= value; }

        [JsonPropertyName("QUERY_MESSAGE")]
        public string? QueryMessage { get; set; }

        [JsonPropertyName("query_message")]
        public string? QueryMessageLower { set => QueryMessage ??= value; }

        [JsonPropertyName("Message")]
        public string? Message { set => QueryMessage ??= value; }

        [JsonPropertyName("message")]
        public string? MessageLower { set => QueryMessage ??= value; }

        [JsonPropertyName("Requirement")]
        public string? Requirement { set => QueryMessage ??= value; }

        [JsonPropertyName("requirement")]
        public string? RequirementLower { set => QueryMessage ??= value; }

        [JsonPropertyName("GLUSR_USR_COMPANYNAME")]
        public string? GlusrUsrCompanyName { get; set; }

        [JsonPropertyName("glusr_usr_companyname")]
        public string? GlusrUsrCompanyNameLower { set => GlusrUsrCompanyName ??= value; }

        [JsonPropertyName("SENDER_COMPANY")]
        public string? SenderCompany { set => GlusrUsrCompanyName ??= value; }

        [JsonPropertyName("sender_company")]
        public string? SenderCompanyLower { set => GlusrUsrCompanyName ??= value; }

        [JsonPropertyName("Company")]
        public string? Company { set => GlusrUsrCompanyName ??= value; }

        [JsonPropertyName("company")]
        public string? CompanyLower { set => GlusrUsrCompanyName ??= value; }

        [JsonPropertyName("SENDER_CITY")]
        public string? SenderCity { get; set; }

        [JsonPropertyName("sender_city")]
        public string? SenderCityLower { set => SenderCity ??= value; }

        [JsonPropertyName("City")]
        public string? City { set => SenderCity ??= value; }

        [JsonPropertyName("city")]
        public string? CityLower { set => SenderCity ??= value; }

        [JsonPropertyName("SENDER_STATE")]
        public string? SenderState { get; set; }

        [JsonPropertyName("sender_state")]
        public string? SenderStateLower { set => SenderState ??= value; }

        [JsonPropertyName("State")]
        public string? State { set => SenderState ??= value; }

        [JsonPropertyName("state")]
        public string? StateLower { set => SenderState ??= value; }

        [JsonPropertyName("SENDER_PINCODE")]
        public string? SenderPincode { get; set; }

        [JsonPropertyName("sender_pincode")]
        public string? SenderPincodeLower { set => SenderPincode ??= value; }

        [JsonPropertyName("Pincode")]
        public string? Pincode { set => SenderPincode ??= value; }

        [JsonPropertyName("pincode")]
        public string? PincodeLower { set => SenderPincode ??= value; }

        [JsonPropertyName("QUERY_TIME")]
        public string? QueryTime { get; set; }

        [JsonPropertyName("query_time")]
        public string? QueryTimeLower { set => QueryTime ??= value; }

        [JsonPropertyName("Timestamp")]
        public string? Timestamp { set => QueryTime ??= value; }

        [JsonPropertyName("timestamp")]
        public string? TimestampLower { set => QueryTime ??= value; }

        [JsonPropertyName("GLUSR_CRM_KEY")]
        public string? GlusrCrmKey { get; set; }

        [JsonPropertyName("glusr_crm_key")]
        public string? GlusrCrmKeyLower { set => GlusrCrmKey ??= value; }

        [JsonPropertyName("ApiKey")]
        public string? ApiKey { set => GlusrCrmKey ??= value; }

        [JsonPropertyName("api_key")]
        public string? ApiKeyLower { set => GlusrCrmKey ??= value; }

        /// <summary>
        /// Collects unknown/unmapped properties so future IndiaMART payload expansions
        /// deserialize cleanly without error.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        /// <summary>
        /// Returns the effective external ID.
        /// </summary>
        public string GetEffectiveExternalKey()
        {
            if (!string.IsNullOrWhiteSpace(UniqueQueryId))
            {
                return UniqueQueryId.Trim();
            }

            if (ExtensionData != null)
            {
                foreach (var key in new[] { "UNIQUE_QUERY_ID", "unique_query_id", "query_id", "lead_id", "id", "enquiry_id" })
                {
                    if (ExtensionData.TryGetValue(key, out var val) && val.ValueKind == JsonValueKind.String)
                    {
                        var s = val.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(s)) return s;
                    }
                }
            }

            var mobile = SenderMobile?.Trim();
            var email = SenderEmail?.Trim();
            return $"{email}|{mobile}".ToLowerInvariant();
        }
    }
}
