namespace CRM.DTO
{
    /// <summary>Payload for POST/PUT leads. Link a company via <see cref="OrganizationId"/>; create or update organizations through <c>/api/organizations</c>.</summary>
    public class LeadUpsertDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>FK to <see cref="CRM.models.Salutation"/>. Omit or <c>0</c> for none; must be active.</summary>
        public int? SalutationId { get; set; }

        public string Gender { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>FK to <see cref="CRM.models.Organization"/>. Omit or use <c>0</c> for no company.</summary>
        public int? OrganizationId { get; set; }

        /// <summary>
        /// Display name used to link an existing organization when <see cref="OrganizationId"/> is omitted
        /// (case-insensitive match on <c>organizations.name</c>).
        /// </summary>
        public string? OrganizationName { get; set; }

        /// <summary>FK to <see cref="CRM.models.LeadStatus"/>.</summary>
        public int? LeadStatusId { get; set; }

        /// <summary>Resolved to <see cref="LeadStatusId"/> by master name when id is not set (e.g. New, Qualified).</summary>
        public string Status { get; set; } = "New";

        /// <summary>FK to <see cref="CRM.models.RequestType"/>. Omit or <c>0</c> for none; must be active.</summary>
        public int? RequestTypeId { get; set; }

        public string Notes { get; set; } = string.Empty;
        public int? LeadOwnerId { get; set; }
        public string LeadSource { get; set; } = "Manual";
        public DateTime? CreatedAt { get; set; }
    }
}
