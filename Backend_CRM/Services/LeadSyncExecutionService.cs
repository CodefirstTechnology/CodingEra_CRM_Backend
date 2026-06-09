using CRM.Configuration;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CRM.Services
{
    public interface ILeadSyncProvider
    {
        string SourceCode { get; }
        bool IsConfigured();
        Task<LeadSyncPullResult> PullLeadsAsync(CancellationToken cancellationToken = default);
    }

    public class LeadSyncPullResult
    {
        public IReadOnlyList<LeadSyncIncomingLead> Leads { get; init; } = Array.Empty<LeadSyncIncomingLead>();
        public string? ErrorMessage { get; init; }
    }

    public class LeadSyncIncomingLead
    {
        public string ExternalKey { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Requirement { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }

    public interface ILeadSyncExecutionService
    {
        Task<LeadSyncExecutionResult> ExecuteAutoSyncAsync(int sourceId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<int>> GetDueAutoSyncSourceIdsAsync(CancellationToken cancellationToken = default);
    }

    public class LeadSyncExecutionResult
    {
        public int TotalReceived { get; set; }
        public int TotalCreated { get; set; }
        public int FailedCount { get; set; }
        public string? ErrorMessage { get; set; }
        public LeadSyncStatus Status { get; set; }
    }

    public class LeadSyncExecutionService : ILeadSyncExecutionService
    {
        private readonly TaskDbcontext _db;
        private readonly ILeadSyncRoundRobinService _roundRobin;
        private readonly IEnumerable<ILeadSyncProvider> _providers;
        private readonly ILogger<LeadSyncExecutionService> _logger;

        public LeadSyncExecutionService(
            TaskDbcontext db,
            ILeadSyncRoundRobinService roundRobin,
            IEnumerable<ILeadSyncProvider> providers,
            ILogger<LeadSyncExecutionService> logger)
        {
            _db = db;
            _roundRobin = roundRobin;
            _providers = providers;
            _logger = logger;
        }

        public async Task<IReadOnlyList<int>> GetDueAutoSyncSourceIdsAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _db.LeadSyncSourceConfigs.AsNoTracking()
                .Where(c => c.AutoSyncEnabled
                    && c.NextSyncAt != null
                    && c.NextSyncAt <= now)
                .Join(
                    _db.LeadSyncSources.Where(s => s.IsActive && s.ApiIntegrationReady),
                    c => c.SourceId,
                    s => s.Id,
                    (c, s) => c.SourceId)
                .ToListAsync(cancellationToken);
        }

        public async Task<LeadSyncExecutionResult> ExecuteAutoSyncAsync(
            int sourceId,
            CancellationToken cancellationToken = default)
        {
            var source = await _db.LeadSyncSources.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sourceId && s.IsActive, cancellationToken);
            if (source == null)
            {
                return new LeadSyncExecutionResult
                {
                    ErrorMessage = "Source not found.",
                    Status = LeadSyncStatus.Failed,
                };
            }

            var provider = _providers.FirstOrDefault(p =>
                string.Equals(p.SourceCode, source.Code, StringComparison.OrdinalIgnoreCase));
            if (provider == null || !provider.IsConfigured())
            {
                return new LeadSyncExecutionResult
                {
                    ErrorMessage = "API integration is not configured for this source.",
                    Status = LeadSyncStatus.Failed,
                };
            }

            var startedAt = DateTime.UtcNow;
            var log = new LeadSyncLog
            {
                SourceId = sourceId,
                SyncType = LeadSyncType.Auto,
                StartedAt = startedAt,
                Status = LeadSyncStatus.Running,
                CreatedAt = startedAt,
            };
            _db.LeadSyncLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);

            LeadSyncExecutionResult result;
            try
            {
                var pull = await provider.PullLeadsAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(pull.ErrorMessage))
                {
                    result = new LeadSyncExecutionResult
                    {
                        TotalReceived = pull.Leads.Count,
                        ErrorMessage = pull.ErrorMessage,
                        Status = LeadSyncStatus.Failed,
                    };
                }
                else
                {
                    result = await PersistIncomingLeadsAsync(source, pull.Leads, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto sync failed for source {SourceId}", sourceId);
                result = new LeadSyncExecutionResult
                {
                    ErrorMessage = ex.Message,
                    Status = LeadSyncStatus.Failed,
                };
            }

            var endedAt = DateTime.UtcNow;
            log.EndedAt = endedAt;
            log.TotalReceived = result.TotalReceived;
            log.TotalCreated = result.TotalCreated;
            log.FailedCount = result.FailedCount;
            log.Status = result.Status;
            log.ErrorMessage = result.ErrorMessage;

            await UpdateSyncTimestampsAsync(sourceId, endedAt, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return result;
        }

        private async Task<LeadSyncExecutionResult> PersistIncomingLeadsAsync(
            LeadSyncSource source,
            IReadOnlyList<LeadSyncIncomingLead> incoming,
            CancellationToken cancellationToken)
        {
            var existingKeys = await BuildExistingKeySetAsync(source.MarkerName, cancellationToken);
            var defaultStatusId = await _db.LeadStatuses.AsNoTracking()
                .Where(s => s.IsActive && s.Name.ToLower() == "new")
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultStatusId <= 0)
            {
                defaultStatusId = await _db.LeadStatuses.AsNoTracking()
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Id)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var created = 0;
            var failed = 0;
            string? lastError = null;

            foreach (var item in incoming)
            {
                var key = $"{source.MarkerName}|ext:{item.ExternalKey}";
                if (existingKeys.Contains(key))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Email) && string.IsNullOrWhiteSpace(item.Mobile))
                {
                    failed++;
                    lastError = "Lead requires email or mobile.";
                    continue;
                }

                var notes = item.Notes;
                if (!string.IsNullOrWhiteSpace(item.Requirement)
                    && (notes == null || !notes.Contains(item.Requirement, StringComparison.Ordinal)))
                {
                    notes = string.IsNullOrWhiteSpace(notes)
                        ? item.Requirement.Trim()
                        : $"{item.Requirement.Trim()}\n{notes}";
                }

                var lead = new Lead
                {
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    Email = item.Email?.Trim() ?? string.Empty,
                    Mobile = item.Mobile?.Trim() ?? string.Empty,
                    Notes = notes ?? string.Empty,
                    LeadSource = "Website",
                    LeadStatusId = defaultStatusId > 0 ? defaultStatusId : null,
                    LeadDate = DateTime.UtcNow.Date,
                    CreatedAt = item.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                await _roundRobin.TryApplyOwnerForSyncLeadAsync(lead, cancellationToken);

                try
                {
                    _db.Leads.Add(lead);
                    await LeadContactSyncHelper.TryAddContactFromLeadAsync(_db, lead);
                    await _db.SaveChangesAsync(cancellationToken);
                    created++;
                    existingKeys.Add(key);
                }
                catch (Exception ex)
                {
                    failed++;
                    lastError = ex.Message;
                    _logger.LogWarning(ex, "Failed to persist sync lead for source {Source}", source.Code);
                }
            }

            var status = failed > 0 && created > 0
                ? LeadSyncStatus.Partial
                : failed > 0
                    ? LeadSyncStatus.Failed
                    : LeadSyncStatus.Completed;

            return new LeadSyncExecutionResult
            {
                TotalReceived = incoming.Count,
                TotalCreated = created,
                FailedCount = failed,
                ErrorMessage = lastError,
                Status = status,
            };
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

                var m = Regex.Match(notes, @"\[crm-ext:[^:\]]+:([^\]]+)\]");
                if (m.Success)
                {
                    keys.Add($"{markerName}|ext:{m.Groups[1].Value.Trim()}");
                }
            }

            return keys;
        }

        private async Task UpdateSyncTimestampsAsync(
            int sourceId,
            DateTime endedAt,
            CancellationToken cancellationToken)
        {
            var config = await _db.LeadSyncSourceConfigs
                .Include(c => c.IntervalOption)
                .FirstOrDefaultAsync(c => c.SourceId == sourceId, cancellationToken);
            if (config == null)
            {
                return;
            }

            config.LastSyncAt = endedAt;
            if (config.AutoSyncEnabled && config.IntervalOption != null)
            {
                config.NextSyncAt = endedAt.AddHours(config.IntervalOption.Hours);
            }
            else
            {
                config.NextSyncAt = null;
            }

            config.UpdatedAt = endedAt;
        }
    }

    public class LeadSyncIndiaMartProvider : ILeadSyncProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LeadSyncIndiaMartOptions _options;

        public LeadSyncIndiaMartProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<LeadSyncIndiaMartOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public string SourceCode => "indiamart";

        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_options.PullApiUrl)
                && !string.IsNullOrWhiteSpace(_options.ApiKey);
        }

        public async Task<LeadSyncPullResult> PullLeadsAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConfigured())
            {
                return new LeadSyncPullResult { ErrorMessage = "IndiaMART API is not configured." };
            }

            var url = BuildPullUrl();
            var client = _httpClientFactory.CreateClient("LeadSyncIndiaMart");
            JsonElement body;
            try
            {
                body = await client.GetFromJsonAsync<JsonElement>(url, cancellationToken);
            }
            catch (Exception ex)
            {
                return new LeadSyncPullResult { ErrorMessage = ex.Message };
            }

            var err = TryGetIndiaMartError(body);
            if (err != null)
            {
                return new LeadSyncPullResult { ErrorMessage = err };
            }

            var leads = MapIndiaMartResponse(body);
            return new LeadSyncPullResult { Leads = leads };
        }

        private string BuildPullUrl()
        {
            var baseUrl = _options.PullApiUrl.Trim();
            var key = Uri.EscapeDataString(_options.ApiKey.Trim());
            var separator = baseUrl.Contains('?') ? '&' : '?';
            if (baseUrl.Contains("glusr_crm_key=", StringComparison.OrdinalIgnoreCase))
            {
                return baseUrl;
            }

            return $"{baseUrl}{separator}glusr_crm_key={key}";
        }

        private static string? TryGetIndiaMartError(JsonElement body)
        {
            if (body.ValueKind != JsonValueKind.Object)
            {
                return "Unexpected IndiaMART response.";
            }

            if (body.TryGetProperty("STATUS", out var status) && status.ValueKind == JsonValueKind.String)
            {
                var s = status.GetString()?.Trim();
                if (!string.Equals(s, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    if (body.TryGetProperty("MESSAGE", out var msg))
                    {
                        return msg.GetString() ?? s;
                    }

                    return s;
                }
            }

            return null;
        }

        private static IReadOnlyList<LeadSyncIncomingLead> MapIndiaMartResponse(JsonElement body)
        {
            var results = new List<LeadSyncIncomingLead>();
            if (!body.TryGetProperty("RESPONSE", out var response))
            {
                return results;
            }

            JsonElement array;
            if (response.ValueKind == JsonValueKind.Array)
            {
                array = response;
            }
            else if (response.TryGetProperty("DATA", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                array = data;
            }
            else
            {
                return results;
            }

            foreach (var row in array.EnumerateArray())
            {
                var mapped = MapIndiaMartRow(row);
                if (mapped != null)
                {
                    results.Add(mapped);
                }
            }

            return results;
        }

        private static LeadSyncIncomingLead? MapIndiaMartRow(JsonElement row)
        {
            static string S(JsonElement el, string name)
            {
                if (!el.TryGetProperty(name, out var v))
                {
                    return string.Empty;
                }

                return v.ValueKind switch
                {
                    JsonValueKind.String => v.GetString()?.Trim() ?? string.Empty,
                    JsonValueKind.Number => v.GetRawText(),
                    _ => string.Empty,
                };
            }

            var name = S(row, "SENDERNAME");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = S(row, "sendername");
            }

            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = parts.Length > 0 ? parts[0] : "Lead";
            var lastName = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : "Contact";

            var mobile = S(row, "SENDERMOBILE");
            if (string.IsNullOrWhiteSpace(mobile))
            {
                mobile = S(row, "MOBILE");
            }

            var email = S(row, "SENDEREMAIL");
            var query = S(row, "QUERY_MESSAGE");
            if (string.IsNullOrWhiteSpace(query))
            {
                query = S(row, "SUBJECT");
            }

            var product = S(row, "PRODUCT_NAME");
            var city = S(row, "SENDER_CITY");
            var extRef = S(row, "UNIQUE_QUERY_ID");
            if (string.IsNullOrWhiteSpace(extRef))
            {
                extRef = S(row, "QUERY_ID");
            }

            if (string.IsNullOrWhiteSpace(extRef))
            {
                extRef = $"{email}|{mobile}".ToLowerInvariant();
            }

            var notesLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(query))
            {
                notesLines.Add(query);
            }

            if (!string.IsNullOrWhiteSpace(product))
            {
                notesLines.Add($"Product: {product}");
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                notesLines.Add($"City: {city}");
            }

            notesLines.Add($"[crm-ext:IndiaMART:{extRef}]");

            return new LeadSyncIncomingLead
            {
                ExternalKey = extRef,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Mobile = mobile,
                Requirement = query,
                Notes = string.Join('\n', notesLines),
            };
        }
    }
}
