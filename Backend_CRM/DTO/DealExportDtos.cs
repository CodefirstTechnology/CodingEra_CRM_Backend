namespace CRM.DTO
{
    /// <summary>
    /// Deal list-style filters plus export-only options (mirrors <see cref="LeadExportRequestDto"/>).
    /// </summary>
    public class DealExportRequestDto
    {
        /// <summary>Status name or pipeline label (same semantics as list <c>status</c>).</summary>
        public string? Status { get; set; }

        /// <summary>Optional FK filter when &gt; 0.</summary>
        public int? StatusId { get; set; }

        /// <summary>Admin-only owner filter (<see cref="CRM.models.Deal.DealOwnerId"/> / assigned user).</summary>
        public int? DealOwnerId { get; set; }

        /// <summary>Text search across contact, org, email, mobile, owner, status, next step.</summary>
        public string? Search { get; set; }

        /// <summary>
        /// <c>all</c>, <c>today</c>, <c>yesterday</c>, <c>this_week</c>, <c>this_month</c>,
        /// <c>last_month</c>, or <c>custom</c>. Applied to <see cref="CRM.models.Deal.CreatedAt"/> date.
        /// </summary>
        public string? DatePreset { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>Ordered export column keys (Deal Listing column ids). Labels optional.</summary>
        public List<DealExportColumnDto> Columns { get; set; } = new();
    }

    public class DealExportColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Label { get; set; }
    }
}
