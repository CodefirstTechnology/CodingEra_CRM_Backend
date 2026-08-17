namespace CRM.DTO
{
    /// <summary>
    /// Shared lead list filters (same fields as <c>GET /api/leads</c>) plus export-only options.
    /// </summary>
    public class LeadExportRequestDto
    {
        /// <summary>Exact <see cref="CRM.models.Lead.LeadSource"/> match when set.</summary>
        public string? LeadSource { get; set; }

        /// <summary>Status id or name (same semantics as list <c>status</c> query).</summary>
        public string? Status { get; set; }

        /// <summary>Admin-only owner filter (same as list <c>leadOwnerId</c>).</summary>
        public int? LeadOwnerId { get; set; }

        /// <summary>Text search across the same fields as the Lead Listing search box.</summary>
        public string? Search { get; set; }

        /// <summary>
        /// <c>all</c>, <c>today</c>, <c>yesterday</c>, <c>this_week</c>, <c>this_month</c>,
        /// <c>last_month</c>, or <c>custom</c>. Applied to <see cref="CRM.models.Lead.LeadDate"/> only.
        /// </summary>
        public string? DatePreset { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>Ordered export column keys (Lead Listing column ids). Labels optional.</summary>
        public List<LeadExportColumnDto> Columns { get; set; } = new();
    }

    public class LeadExportColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Label { get; set; }
    }
}
