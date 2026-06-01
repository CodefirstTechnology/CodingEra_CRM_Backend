namespace CRM.DTO
{
    /// <summary>
    /// Create/update body for <c>POST/PUT /api/organizations</c>.
    /// Use master <b>ids only</b> (<see cref="IndustryId"/>, etc.); do not send nested industry/employee/territory objects.
    /// </summary>
    public class OrganizationUpsertDto
    {
        /// <summary>Optional on POST; must match route id on PUT when sent.</summary>
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Gst { get; set; } = string.Empty;
        public decimal? AnnualRevenue { get; set; }

        /// <summary>FK to master industry; omit or null to clear. <c>0</c> is treated as unset.</summary>
        public int? IndustryId { get; set; }

        /// <summary>FK to master employee-count band.</summary>
        public int? EmployeeCountId { get; set; }

        /// <summary>FK to master territory.</summary>
        public int? TerritoryId { get; set; }
    }
}
