using CRM.DTO;
using CRM.Services;

namespace CRM.Helpers
{
    /// <summary>
    /// Maps external integration payloads into <see cref="LeadSyncIncomingLead"/>
    /// for <see cref="IMarketplaceLeadPersistenceService"/>.
    /// </summary>
    public static class MarketplaceLeadMapper
    {
        public const string JustdialMarkerName = "Justdial";
        public const string JustdialLeadSource = "Justdial";

        public const string IndiaMartMarkerName = "IndiaMART";
        public const string IndiaMartLeadSource = "IndiaMART";

        public static LeadSyncIncomingLead FromIndiaMartPush(IndiaMartWebhookLeadDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var extKey = dto.GetEffectiveExternalKey();
            var fullName = (dto.SenderName ?? string.Empty).Trim();
            var (firstName, lastName) = SplitName(fullName);

            var product = dto.QueryProductName?.Trim() ?? dto.Subject?.Trim();
            var requirement = dto.QueryMessage?.Trim() ?? product;
            var company = dto.GlusrUsrCompanyName?.Trim();
            var city = dto.SenderCity?.Trim();

            var notesLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(requirement))
            {
                notesLines.Add(requirement);
            }
            if (!string.IsNullOrWhiteSpace(product) && !string.Equals(product, requirement, StringComparison.OrdinalIgnoreCase))
            {
                notesLines.Add($"Product: {product}");
            }
            if (!string.IsNullOrWhiteSpace(company))
            {
                notesLines.Add($"Company: {company}");
            }
            if (!string.IsNullOrWhiteSpace(city))
            {
                notesLines.Add($"City: {city}");
            }

            notesLines.Add(LeadSyncNotesHelper.FormatExtMarker(IndiaMartMarkerName, extKey));

            DateTime? createdAt = null;
            if (!string.IsNullOrWhiteSpace(dto.QueryTime)
                && DateTime.TryParse(dto.QueryTime, out var parsedTime))
            {
                createdAt = parsedTime;
            }

            return new LeadSyncIncomingLead
            {
                ExternalKey = extKey,
                FirstName = firstName,
                LastName = lastName,
                Email = dto.SenderEmail?.Trim() ?? string.Empty,
                Mobile = dto.SenderMobile?.Trim() ?? string.Empty,
                Requirement = requirement,
                OrganizationName = string.IsNullOrWhiteSpace(company) ? null : company,
                Notes = string.Join('\n', notesLines),
                CreatedAt = createdAt
            };
        }

        public static LeadSyncIncomingLead FromJustdial(JustdialWebhookLeadDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var leadId = (dto.Leadid ?? string.Empty).Trim();
            var fullName = (dto.Name ?? string.Empty).Trim();
            var (firstName, lastName) = SplitName(fullName);

            return new LeadSyncIncomingLead
            {
                ExternalKey = leadId,
                FirstName = firstName,
                LastName = lastName,
                Email = dto.Email?.Trim() ?? string.Empty,
                Mobile = dto.Mobile?.Trim() ?? string.Empty,
                Requirement = null,
                // Notes hold only the marketplace marker (dedupe + round-robin).
                Notes = LeadSyncNotesHelper.FormatExtMarker(JustdialMarkerName, leadId),
                CreatedAt = null
            };
        }

        private static (string FirstName, string LastName) SplitName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return ("Lead", "Contact");
            }

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return ("Lead", "Contact");
            }

            if (parts.Length == 1)
            {
                return (parts[0], "Contact");
            }

            return (parts[0], string.Join(' ', parts.Skip(1)));
        }
    }
}
