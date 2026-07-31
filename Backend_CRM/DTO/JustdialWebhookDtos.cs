using System.Text.Json.Serialization;
using CRM.Serialization;

namespace CRM.DTO
{
    /// <summary>
    /// Inbound Justdial webhook lead payload. Field names match Justdial documentation exactly.
    /// Unknown extra fields (e.g. future Justdial additions) are ignored by System.Text.Json.
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

        /// <summary>Justdial documents as int (0/1); also accepts string "0"/"1".</summary>
        [JsonPropertyName("dncmobile")]
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Dncmobile { get; set; }

        /// <summary>Justdial documents as int (0/1); also accepts string "0"/"1".</summary>
        [JsonPropertyName("dncphone")]
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Dncphone { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        /// <summary>May arrive as string or number in JSON.</summary>
        [JsonPropertyName("pincode")]
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Pincode { get; set; }

        [JsonPropertyName("time")]
        public string? Time { get; set; }

        /// <summary>May arrive as string or number in JSON.</summary>
        [JsonPropertyName("branchpin")]
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Branchpin { get; set; }

        [JsonPropertyName("parentid")]
        public string? Parentid { get; set; }

        /// <summary>Optional newer Justdial field; ignored by lead mapping when present.</summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }
    }
}
