using System.Text.Json.Serialization;

namespace CRM.DTO
{
    /// <summary>
    /// Inbound Justdial webhook lead payload. Field names match Justdial documentation exactly.
    /// </summary>
    public class JustdialWebhookLeadDto
    {
        [JsonPropertyName("leadid")]
        public string? Leadid { get; set; }

        [JsonPropertyName("leadtype")]
        public string? Leadtype { get; set; }

        [JsonPropertyName("prefix")]
        public string? Prefix { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("area")]
        public string? Area { get; set; }

        [JsonPropertyName("brancharea")]
        public string? Brancharea { get; set; }

        [JsonPropertyName("dncmobile")]
        public string? Dncmobile { get; set; }

        [JsonPropertyName("dncphone")]
        public string? Dncphone { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("pincode")]
        public string? Pincode { get; set; }

        [JsonPropertyName("time")]
        public string? Time { get; set; }

        [JsonPropertyName("branchpin")]
        public string? Branchpin { get; set; }

        [JsonPropertyName("parentid")]
        public string? Parentid { get; set; }
    }
}
