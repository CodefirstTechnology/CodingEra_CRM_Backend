namespace CRM.Configuration
{
    /// <summary>
    /// Production configuration for inbound IndiaMART Push / Webhook integration.
    /// Bound from configuration section <see cref="SectionName"/>.
    /// </summary>
    public class IndiaMartWebhookOptions
    {
        public const string SectionName = "IndiaMartWebhook";

        /// <summary>
        /// Kill-switch. When false, the endpoint rejects requests with 503 without processing leads.
        /// Default is false in production until explicitly configured.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>Shared secret for webhook authentication.</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// When true, incoming webhook requests must present a valid API key.
        /// </summary>
        public bool RequireApiKey { get; set; } = true;

        /// <summary>HTTP header name inspected for the webhook secret.</summary>
        public string ApiKeyHeaderName { get; set; } = "X-IndiaMart-Webhook-Key";

        /// <summary>
        /// When true, query parameter API key lookup is permitted.
        /// Default is false to prevent credentials in access logs.
        /// </summary>
        public bool AllowApiKeyQueryParameter { get; set; } = false;

        /// <summary>Query parameter name if query-based auth is enabled.</summary>
        public string ApiKeyQueryParameterName { get; set; } = "api_key";

        /// <summary>When true, remote IP must match <see cref="AllowedIpAddresses"/>.</summary>
        public bool RequireIpWhitelist { get; set; } = false;

        /// <summary>Allowed client IPs (IPv4/IPv6).</summary>
        public List<string> AllowedIpAddresses { get; set; } = new();

        /// <summary>When true, logs a redacted payload snapshot for troubleshooting.</summary>
        public bool EnableDetailedPayloadLogging { get; set; } = false;

        /// <summary>Reject request bodies larger than this byte limit (default: 1MB).</summary>
        public long MaxRequestBodyBytes { get; set; } = 1_048_576;

        /// <summary>Soft processing timeout applied after request is accepted.</summary>
        public int ProcessingTimeoutSeconds { get; set; } = 30;

        /// <summary>Header used to propagate / accept correlation IDs.</summary>
        public string CorrelationIdHeaderName { get; set; } = "X-Correlation-Id";

        /// <summary>Publicly reachable base URL of the CRM (e.g. https://crm.example.com).</summary>
        public string PublicBaseUrl { get; set; } = string.Empty;
    }
}
