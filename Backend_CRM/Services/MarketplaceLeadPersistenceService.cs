using System.Text.RegularExpressions;
using CRM.DATA;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public enum MarketplaceLeadPersistOutcome
    {
        Created,
        Duplicate,
        ValidationFailed,
        Failed
    }

    public sealed class MarketplaceLeadPersistItemResult
    {
        public MarketplaceLeadPersistOutcome Outcome { get; init; }
        public string ExternalKey { get; init; } = string.Empty;
        public int? LeadId { get; init; }
        public int? LeadOwnerId { get; init; }
        public bool RoundRobinAssigned { get; init; }
        public bool ContactCreated { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public sealed class MarketplaceLeadPersistBatchResult
    {
        public int TotalReceived { get; init; }
        public int TotalCreated { get; init; }
        public int DuplicateCount { get; init; }
        public int FailedCount { get; init; }
        public string? ErrorMessage { get; init; }
        public LeadSyncStatus Status { get; init; }
        public IReadOnlyList<MarketplaceLeadPersistItemResult> Items { get; init; }
            = Array.Empty<MarketplaceLeadPersistItemResult>();
    }

    public interface IMarketplaceLeadPersistenceService
    {
        /// <summary>
        /// Persists inbound marketplace / integration leads using shared CRM rules
        /// (dedupe marker, status, round-robin, contact sync, SaveChanges/activity).
        /// </summary>
        Task<MarketplaceLeadPersistBatchResult> PersistAsync(
            string markerName,
            string leadSource,
            IReadOnlyList<LeadSyncIncomingLead> incoming,
            CancellationToken cancellationToken = default);

        Task<MarketplaceLeadPersistItemResult> PersistOneAsync(
            string markerName,
            string leadSource,
            LeadSyncIncomingLead incoming,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Single persistence path for external marketplace and webhook leads.
    /// </summary>
    public sealed class MarketplaceLeadPersistenceService : IMarketplaceLeadPersistenceService
    {
        private static readonly Regex ExtIdRegex = new(
            @"\[crm-ext:[^:\]]+:([^\]]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly TaskDbcontext _db;
        private readonly ILeadSyncRoundRobinService _roundRobin;
        private readonly ILogger<MarketplaceLeadPersistenceService> _logger;

        public MarketplaceLeadPersistenceService(
            TaskDbcontext db,
            ILeadSyncRoundRobinService roundRobin,
            ILogger<MarketplaceLeadPersistenceService> logger)
        {
            _db = db;
            _roundRobin = roundRobin;
            _logger = logger;
        }

        public async Task<MarketplaceLeadPersistItemResult> PersistOneAsync(
            string markerName,
            string leadSource,
            LeadSyncIncomingLead incoming,
            CancellationToken cancellationToken = default)
        {
            var batch = await PersistAsync(
                markerName,
                leadSource,
                new[] { incoming },
                cancellationToken);

            return batch.Items.Count > 0
                ? batch.Items[0]
                : new MarketplaceLeadPersistItemResult
                {
                    Outcome = MarketplaceLeadPersistOutcome.Failed,
                    ExternalKey = incoming.ExternalKey,
                    ErrorMessage = batch.ErrorMessage ?? "Persist returned no item result."
                };
        }

        public async Task<MarketplaceLeadPersistBatchResult> PersistAsync(
            string markerName,
            string leadSource,
            IReadOnlyList<LeadSyncIncomingLead> incoming,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(markerName))
            {
                throw new ArgumentException("markerName is required.", nameof(markerName));
            }

            if (string.IsNullOrWhiteSpace(leadSource))
            {
                throw new ArgumentException("leadSource is required.", nameof(leadSource));
            }

            var existingKeys = await BuildExistingKeySetAsync(markerName.Trim(), cancellationToken);
            var defaultStatusId = await ResolveDefaultStatusIdAsync(cancellationToken);

            var created = 0;
            var duplicates = 0;
            var failed = 0;
            string? lastError = null;
            var items = new List<MarketplaceLeadPersistItemResult>(incoming.Count);

            foreach (var item in incoming)
            {
                var itemResult = await PersistSingleAsync(
                    markerName.Trim(),
                    leadSource.Trim(),
                    item,
                    existingKeys,
                    defaultStatusId,
                    cancellationToken);

                items.Add(itemResult);

                switch (itemResult.Outcome)
                {
                    case MarketplaceLeadPersistOutcome.Created:
                        created++;
                        break;
                    case MarketplaceLeadPersistOutcome.Duplicate:
                        duplicates++;
                        break;
                    default:
                        failed++;
                        lastError = itemResult.ErrorMessage ?? lastError;
                        break;
                }
            }

            var status = failed > 0 && created > 0
                ? LeadSyncStatus.Partial
                : failed > 0
                    ? LeadSyncStatus.Failed
                    : LeadSyncStatus.Completed;

            return new MarketplaceLeadPersistBatchResult
            {
                TotalReceived = incoming.Count,
                TotalCreated = created,
                DuplicateCount = duplicates,
                FailedCount = failed,
                ErrorMessage = lastError,
                Status = status,
                Items = items
            };
        }

        private async Task<MarketplaceLeadPersistItemResult> PersistSingleAsync(
            string markerName,
            string leadSource,
            LeadSyncIncomingLead item,
            HashSet<string> existingKeys,
            int defaultStatusId,
            CancellationToken cancellationToken)
        {
            var externalKey = item.ExternalKey?.Trim() ?? string.Empty;
            var key = $"{markerName}|ext:{externalKey}";

            if (existingKeys.Contains(key))
            {
                _logger.LogInformation(
                    "Duplicate detected for marketplace lead. Marker={Marker} ExternalKey={ExternalKey}",
                    markerName,
                    externalKey);

                return new MarketplaceLeadPersistItemResult
                {
                    Outcome = MarketplaceLeadPersistOutcome.Duplicate,
                    ExternalKey = externalKey,
                    ErrorMessage = "Duplicate marketplace lead."
                };
            }

            if (string.IsNullOrWhiteSpace(item.Email) && string.IsNullOrWhiteSpace(item.Mobile))
            {
                return new MarketplaceLeadPersistItemResult
                {
                    Outcome = MarketplaceLeadPersistOutcome.ValidationFailed,
                    ExternalKey = externalKey,
                    ErrorMessage = "Lead requires email or mobile."
                };
            }

            var notes = item.Notes;
            if (!string.IsNullOrWhiteSpace(item.Requirement)
                && (notes == null || !notes.Contains(item.Requirement, StringComparison.Ordinal)))
            {
                notes = string.IsNullOrWhiteSpace(notes)
                    ? item.Requirement.Trim()
                    : $"{item.Requirement.Trim()}\n{notes}";
            }

            int? organizationId = null;
            if (!string.IsNullOrWhiteSpace(item.OrganizationName))
            {
                organizationId = await FindOrCreateOrganizationIdAsync(
                    item.OrganizationName.Trim(),
                    cancellationToken);
            }

            var lead = new Lead
            {
                FirstName = item.FirstName,
                LastName = item.LastName,
                Email = item.Email?.Trim() ?? string.Empty,
                Mobile = item.Mobile?.Trim() ?? string.Empty,
                Notes = notes ?? string.Empty,
                OrganizationId = organizationId,
                LeadSource = leadSource,
                LeadStatusId = defaultStatusId > 0 ? defaultStatusId : null,
                LeadDate = DateTime.UtcNow.Date,
                CreatedAt = item.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var roundRobinAssigned = await _roundRobin.TryApplyOwnerForSyncLeadAsync(lead, cancellationToken);
            if (roundRobinAssigned)
            {
                _logger.LogInformation(
                    "Round Robin assigned owner {OwnerId} for marketplace lead ExternalKey={ExternalKey}",
                    lead.LeadOwnerId,
                    externalKey);
            }

            try
            {
                _db.Leads.Add(lead);
                await LeadContactSyncHelper.TryAddContactFromLeadAsync(_db, lead, cancellationToken: cancellationToken);

                var contactCreated = _db.ChangeTracker.Entries<Contact>()
                    .Any(e => e.State == EntityState.Added);

                await _db.SaveChangesAsync(cancellationToken);

                existingKeys.Add(key);

                _logger.LogInformation(
                    "Marketplace lead inserted. LeadId={LeadId} Source={LeadSource} ExternalKey={ExternalKey} ContactCreated={ContactCreated}",
                    lead.Id,
                    leadSource,
                    externalKey,
                    contactCreated);

                return new MarketplaceLeadPersistItemResult
                {
                    Outcome = MarketplaceLeadPersistOutcome.Created,
                    ExternalKey = externalKey,
                    LeadId = lead.Id,
                    LeadOwnerId = lead.LeadOwnerId,
                    RoundRobinAssigned = roundRobinAssigned,
                    ContactCreated = contactCreated
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Database error while persisting marketplace lead. Marker={Marker} ExternalKey={ExternalKey}",
                    markerName,
                    externalKey);

                DetachAddedMarketplaceEntities();

                return new MarketplaceLeadPersistItemResult
                {
                    Outcome = MarketplaceLeadPersistOutcome.Failed,
                    ExternalKey = externalKey,
                    RoundRobinAssigned = roundRobinAssigned,
                    ErrorMessage = ex.Message
                };
            }
        }

        private void DetachAddedMarketplaceEntities()
        {
            foreach (var entry in _db.ChangeTracker.Entries()
                         .Where(e => e.State == EntityState.Added)
                         .ToList())
            {
                entry.State = EntityState.Detached;
            }
        }

        private async Task<int> ResolveDefaultStatusIdAsync(CancellationToken cancellationToken)
        {
            var defaultStatusId = await _db.LeadStatuses.AsNoTracking()
                .Where(s => s.IsActive && s.Name.ToLower() == "new")
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultStatusId > 0)
            {
                return defaultStatusId;
            }

            return await _db.LeadStatuses.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Id)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<HashSet<string>> BuildExistingKeySetAsync(
            string markerName,
            CancellationToken cancellationToken)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var leads = await _db.Leads.AsNoTracking()
                .Where(l => l.Notes != null && l.Notes.Contains("[crm-ext:"))
                .Select(l => l.Notes!)
                .ToListAsync(cancellationToken);

            foreach (var notes in leads)
            {
                var marker = LeadSyncNotesHelper.TryExtractMarkerName(notes);
                if (marker == null || !string.Equals(marker, markerName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var m = ExtIdRegex.Match(notes);
                if (m.Success)
                {
                    keys.Add($"{markerName}|ext:{m.Groups[1].Value.Trim()}");
                }
            }

            return keys;
        }

        private async Task<int?> FindOrCreateOrganizationIdAsync(
            string organizationName,
            CancellationToken cancellationToken)
        {
            var trimmed = organizationName.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            var tl = trimmed.ToLowerInvariant();
            var existingId = await _db.Organizations.AsNoTracking()
                .Where(o => o.Name.ToLower() == tl)
                .OrderBy(o => o.Id)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingId is > 0)
            {
                return existingId;
            }

            var now = DateTime.UtcNow;
            var org = new Organization
            {
                Name = trimmed,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                LastModified = now,
            };
            _db.Organizations.Add(org);
            await _db.SaveChangesAsync(cancellationToken);
            return org.Id;
        }
    }
}
