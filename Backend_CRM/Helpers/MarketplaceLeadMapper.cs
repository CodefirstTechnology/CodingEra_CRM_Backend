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
