namespace CRM.DTO
{
    /// <summary>Optional nested organization on lead create/update (create new row or link by <c>Id</c>).</summary>
    public class LeadOrganizationInputDto
    {
        /// <summary>When set, links this existing organization (no other fields required).</summary>
        public int? Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public decimal? AnnualRevenue { get; set; }
        public string Employees { get; set; } = string.Empty;
        public string Territory { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    /// <summary>Payload for POST/PUT leads (company data is on <see cref="CRM.models.Organization"/> via FK or nested <see cref="LeadOrganizationInputDto"/>).</summary>
    public class LeadUpsertDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Salutation { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int? OrganizationId { get; set; }

        /// <summary>When provided, creates a new organization or links by <see cref="LeadOrganizationInputDto.Id"/>.</summary>
        public LeadOrganizationInputDto? Organization { get; set; }

        public string JobTitle { get; set; } = string.Empty;
        public string Status { get; set; } = "New";
        public string RequestType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string LeadOwnerName { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public int? LeadOwnerId { get; set; }
        public string LeadSource { get; set; } = "Manual";
        public long? SortTimestamp { get; set; }
        public string? ExternalRef { get; set; }
        public string? Product { get; set; }
        public int? Quantity { get; set; }
        public string? Message { get; set; }
        public string? City { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
