using System.Globalization;
using System.Text.Json;
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
            var fullName = (dto.GetEffectiveSenderName() ?? string.Empty).Trim();
            var (firstName, lastName) = SplitName(fullName);

            var product = dto.GetEffectiveProductName()?.Trim();
            var requirement = dto.GetEffectiveMessage()?.Trim() ?? product;
            var company = dto.GetEffectiveCompanyName()?.Trim();
            var city = dto.SenderCity?.Trim();
            var state = dto.SenderState?.Trim();
            var pincode = dto.SenderPincode?.Trim();
            var phone = dto.SenderPhone?.Trim();
            var address = dto.SenderAddress?.Trim();
            var mcat = dto.QueryMcatName?.Trim();
            var queryType = dto.QueryType?.Trim();

            var notesLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(requirement))
            {
                notesLines.Add(requirement);
            }
            if (!string.IsNullOrWhiteSpace(product) && !string.Equals(product, requirement, StringComparison.OrdinalIgnoreCase))
            {
                notesLines.Add($"Product: {product}");
            }
            if (!string.IsNullOrWhiteSpace(mcat) && !string.Equals(mcat, product, StringComparison.OrdinalIgnoreCase))
            {
                notesLines.Add($"Category: {mcat}");
            }
            if (!string.IsNullOrWhiteSpace(company))
            {
                notesLines.Add($"Company: {company}");
            }
            if (!string.IsNullOrWhiteSpace(phone))
            {
                notesLines.Add($"Phone: {phone}");
            }
            if (!string.IsNullOrWhiteSpace(address))
            {
                notesLines.Add($"Address: {address}");
            }
            if (!string.IsNullOrWhiteSpace(city))
            {
                notesLines.Add($"City: {city}");
            }
            if (!string.IsNullOrWhiteSpace(state))
            {
                notesLines.Add($"State: {state}");
            }
            if (!string.IsNullOrWhiteSpace(pincode))
            {
                notesLines.Add($"Pincode: {pincode}");
            }
            if (!string.IsNullOrWhiteSpace(queryType))
            {
                notesLines.Add($"Query Type: {queryType}");
            }

            notesLines.Add(LeadSyncNotesHelper.FormatExtMarker(IndiaMartMarkerName, extKey));

            DateTime? createdAt = null;
            if (!string.IsNullOrWhiteSpace(dto.QueryTime)
                && DateTime.TryParse(dto.QueryTime, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedTime))
            {
                if (parsedTime.Kind == DateTimeKind.Unspecified)
                {
                    // IndiaMART timestamps are Indian Standard Time (IST = UTC+05:30)
                    var istOffset = TimeSpan.FromHours(5.5);
                    var dtoUtc = new DateTimeOffset(parsedTime, istOffset).UtcDateTime;
                    createdAt = DateTime.SpecifyKind(dtoUtc, DateTimeKind.Utc);
                }
                else
                {
                    createdAt = parsedTime.ToUniversalTime();
                }
            }

            return new LeadSyncIncomingLead
            {
                ExternalKey = extKey,
                FirstName = firstName,
                LastName = lastName,
                Email = dto.SenderEmail?.Trim() ?? string.Empty,
                Mobile = dto.GetEffectiveSenderMobile() ?? string.Empty,
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

            var rawMobile = dto.Mobile?.Trim();
            var rawPhone = dto.Phone?.Trim();
            var primaryMobile = !string.IsNullOrWhiteSpace(rawMobile)
                ? rawMobile!
                : (!string.IsNullOrWhiteSpace(rawPhone) ? rawPhone! : string.Empty);

            var hasDistinctPhone = !string.IsNullOrWhiteSpace(rawMobile)
                && !string.IsNullOrWhiteSpace(rawPhone)
                && !string.Equals(rawMobile, rawPhone, StringComparison.OrdinalIgnoreCase);

            var company = dto.Company?.Trim();

            var notesLines = new List<string>();
            var category = dto.Category?.Trim();
            if (!string.IsNullOrWhiteSpace(category))
            {
                notesLines.Add(category);
            }
            notesLines.Add(LeadSyncNotesHelper.FormatExtMarker(JustdialMarkerName, leadId));
            var notes = string.Join('\n', notesLines);

            var effectiveArea = !string.IsNullOrWhiteSpace(dto.Area)
                ? dto.Area.Trim()
                : (!string.IsNullOrWhiteSpace(dto.Brancharea) ? dto.Brancharea.Trim() : null);

            var effectiveCity = !string.IsNullOrWhiteSpace(dto.City) ? dto.City.Trim() : null;
            var effectiveState = !string.IsNullOrWhiteSpace(dto.State) ? dto.State.Trim() : null;

            var effectivePin = !string.IsNullOrWhiteSpace(dto.Pincode) && dto.Pincode.Trim() != "0"
                ? dto.Pincode.Trim()
                : (!string.IsNullOrWhiteSpace(dto.Branchpin) && dto.Branchpin.Trim() != "0" ? dto.Branchpin.Trim() : null);

            var addressParts = new[] { effectiveArea, effectiveCity, effectiveState }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            string? location = null;
            if (addressParts.Count > 0 && !string.IsNullOrWhiteSpace(effectivePin))
            {
                location = $"{string.Join(", ", addressParts)} - {effectivePin}";
            }
            else if (addressParts.Count > 0)
            {
                location = string.Join(", ", addressParts);
            }
            else if (!string.IsNullOrWhiteSpace(effectivePin))
            {
                location = effectivePin;
            }

            var rawDict = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(dto.Leadtype))
            {
                rawDict["leadtype"] = dto.Leadtype.Trim();
            }
            if (!string.IsNullOrWhiteSpace(dto.Prefix))
            {
                rawDict["prefix"] = dto.Prefix.Trim();
            }
            if (!string.IsNullOrWhiteSpace(dto.Parentid))
            {
                rawDict["parentid"] = dto.Parentid.Trim();
            }
            if (!string.IsNullOrWhiteSpace(dto.Dncmobile))
            {
                rawDict["dncmobile"] = dto.Dncmobile.Trim();
            }
            if (!string.IsNullOrWhiteSpace(dto.Dncphone))
            {
                rawDict["dncphone"] = dto.Dncphone.Trim();
            }
            if (!string.IsNullOrWhiteSpace(dto.Date))
            {
                rawDict["raw_date"] = dto.Date.Trim();
            }
            if (!string.IsNullOrWhiteSpace(dto.Time))
            {
                rawDict["raw_time"] = dto.Time.Trim();
            }
            if (hasDistinctPhone)
            {
                rawDict["phone"] = rawPhone!;
            }

            string? rawPayload = rawDict.Count > 0 ? JsonSerializer.Serialize(rawDict) : null;

            var createdAt = ParseJustdialDateTime(dto.Date, dto.Time);

            return new LeadSyncIncomingLead
            {
                ExternalKey = leadId,
                FirstName = firstName,
                LastName = lastName,
                Email = dto.Email?.Trim() ?? string.Empty,
                Mobile = primaryMobile,
                Requirement = string.IsNullOrWhiteSpace(category) ? null : category,
                OrganizationName = string.IsNullOrWhiteSpace(company) ? null : company,
                Location = location,
                RawPayload = rawPayload,
                Notes = notes,
                CreatedAt = createdAt
            };
        }

        private static DateTime? ParseJustdialDateTime(string? dateStr, string? timeStr)
        {
            var date = dateStr?.Trim();
            var time = timeStr?.Trim();

            if (string.IsNullOrWhiteSpace(date))
            {
                return null;
            }

            var combined = !string.IsNullOrWhiteSpace(time) ? $"{date} {time}" : date;

            if (DateTime.TryParse(combined, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                || DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                if (parsed.Kind == DateTimeKind.Unspecified)
                {
                    // Justdial timestamps are Indian Standard Time (IST = UTC+05:30)
                    var istOffset = TimeSpan.FromHours(5.5);
                    var utc = new DateTimeOffset(parsed, istOffset).UtcDateTime;
                    return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
                }

                return parsed.ToUniversalTime();
            }

            return null;
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
