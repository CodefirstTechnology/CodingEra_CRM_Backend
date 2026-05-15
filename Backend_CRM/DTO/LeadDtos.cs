namespace CRM.DTO
{
    /// <summary>Optional nested organization on lead create/update (create new row or link by <c>Id</c>).</summary>
    public class LeadOrganizationInputDto
    {
        /// <summary>When set, links this existing organization (no other fields required).</summary>
        public int? Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;

        /// <summary>FK to <see cref="CRM.models.Industry"/>; takes precedence when set.</summary>
        public int? IndustryId { get; set; }

        /// <summary>Resolved when <see cref="IndustryId"/> is null and this matches a master name (case-insensitive).</summary>
        public string Industry { get; set; } = string.Empty;

        public decimal? AnnualRevenue { get; set; }

        /// <summary>FK to <see cref="CRM.models.EmployeeCount"/>.</summary>
        public int? EmployeeCountId { get; set; }

        /// <summary>Resolved to <see cref="EmployeeCountId"/> by name when id is not set.</summary>
        public string Employees { get; set; } = string.Empty;

        /// <summary>FK to <see cref="CRM.models.Territory"/>.</summary>
        public int? TerritoryId { get; set; }

        /// <summary>Resolved to <see cref="TerritoryId"/> by name when id is not set.</summary>
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

        /// <summary>FK to <see cref="CRM.models.Salutation"/>; takes precedence when set.</summary>
        public int? SalutationId { get; set; }

        /// <summary>Resolved to <see cref="SalutationId"/> by master name when id is not set.</summary>
        public string Salutation { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int? OrganizationId { get; set; }

        /// <summary>When provided, creates a new organization or links by <see cref="LeadOrganizationInputDto.Id"/>.</summary>
        public LeadOrganizationInputDto? Organization { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        /// <summary>FK to <see cref="CRM.models.LeadStatus"/>.</summary>
        public int? LeadStatusId { get; set; }

        /// <summary>Resolved to <see cref="LeadStatusId"/> by master name when id is not set (e.g. New, Qualified).</summary>
        public string Status { get; set; } = "New";

        /// <summary>FK to <see cref="CRM.models.RequestType"/>.</summary>
        public int? RequestTypeId { get; set; }

        /// <summary>Resolved to <see cref="RequestTypeId"/> by master name when id is not set.</summary>
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
