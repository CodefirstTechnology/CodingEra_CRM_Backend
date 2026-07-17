namespace CRM.Configuration
{
    /// <summary>
    /// Production settings for the Justdial inbound webhook.
    /// Bound from configuration section <see cref="SectionName"/>.
    /// </summary>
    public class JustdialWebhookOptions
    {
        public const string SectionName = "JustdialWebhook";

        /// <summary>When false, the endpoint rejects requests without processing leads.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Shared secret compared to the configured API key header/query value.</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// When true, requests must present a valid API key.
        /// When false, API key checks are skipped (local/dev only).
        /// </summary>
        public bool RequireApiKey { get; set; } = true;

        public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

        /// <summary>Allow API key via query string (useful for Justdial GET pushes).</summary>
        public bool AllowApiKeyQueryParameter { get; set; } = true;

        public string ApiKeyQueryParameterName { get; set; } = "api_key";

        /// <summary>When true, remote IP must match <see cref="AllowedIpAddresses"/>.</summary>
        public bool RequireIpWhitelist { get; set; } = false;

        /// <summary>Allowed client IPs or CIDR-less exact addresses (IPv4/IPv6).</summary>
        public List<string> AllowedIpAddresses { get; set; } = new();

        /// <summary>When true, logs a redacted payload snapshot for troubleshooting.</summary>
        public bool EnableDetailedPayloadLogging { get; set; } = false;

        /// <summary>Reject POST bodies larger than this many bytes.</summary>
        public long MaxRequestBodyBytes { get; set; } = 65_536;

        /// <summary>Soft processing timeout applied after the request is accepted.</summary>
        public int ProcessingTimeoutSeconds { get; set; } = 30;

        /// <summary>Header used to propagate / accept a correlation id.</summary>
        public string CorrelationIdHeaderName { get; set; } = "X-Correlation-Id";
    }
}
