using System;
using System.Text.Json.Serialization;

namespace ERP.Application.Procurement.Dtos
{
    public class AuditTrailEntryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("module")]
        public string Module { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("entityId")]
        public int EntityId { get; set; }

        [JsonPropertyName("entityNumber")]
        public string EntityNumber { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("oldValue")]
        public string OldValue { get; set; } = string.Empty;

        [JsonPropertyName("newValue")]
        public string NewValue { get; set; } = string.Empty;

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;
    }

    public class AuditTrailFilterQueryDto
    {
        public string? Search { get; set; }
        public string? Module { get; set; }
        public string? Action { get; set; }
        public string? User { get; set; }
        public int? EntityId { get; set; }
        public string? EntityNumber { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class AuditTrailRecordInputDto
    {
        [JsonPropertyName("module")]
        public string Module { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("entityId")]
        public int EntityId { get; set; }

        [JsonPropertyName("entityNumber")]
        public string EntityNumber { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public string? User { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("oldValue")]
        public string? OldValue { get; set; }

        [JsonPropertyName("newValue")]
        public string? NewValue { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }
}
