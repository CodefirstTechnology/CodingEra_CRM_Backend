using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CRM.Services
{
    public interface ILeadSyncProvider
    {
        string SourceCode { get; }
        bool IsConfigured(LeadSyncResolvedCredentials credentials);
        Task<LeadSyncPullResult> PullLeadsAsync(
            LeadSyncResolvedCredentials credentials,
            CancellationToken cancellationToken = default);
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
        Task<LeadSyncExecutionResult> ExecuteManualSyncAsync(
            int sourceId,
            int userId,
            CancellationToken cancellationToken = default);
        Task<LeadSyncExecutionResult> TestConnectionAsync(int sourceId, CancellationToken cancellationToken = default);
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
        private readonly ILeadSyncCredentialService _credentials;
        private readonly IMarketplaceLeadPersistenceService _marketplacePersistence;
        private readonly IEnumerable<ILeadSyncProvider> _providers;
        private readonly ILogger<LeadSyncExecutionService> _logger;

        public LeadSyncExecutionService(
            TaskDbcontext db,
            ILeadSyncCredentialService credentials,
            IMarketplaceLeadPersistenceService marketplacePersistence,
            IEnumerable<ILeadSyncProvider> providers,
            ILogger<LeadSyncExecutionService> logger)
        {
            _db = db;
            _credentials = credentials;
            _marketplacePersistence = marketplacePersistence;
            _providers = providers;
            _logger = logger;
        }

        public async Task<IReadOnlyList<int>> GetDueAutoSyncSourceIdsAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _db.LeadSyncSourceConfigs.AsNoTracking()
                .Where(c => c.AutoSyncEnabled
                    && (c.NextSyncAt == null || c.NextSyncAt <= now))
                .Join(
                    _db.LeadSyncSources.Where(s => s.IsActive),
                    c => c.SourceId,
                    s => s.Id,
                    (c, s) => new { c.SourceId, s.Id })
                .Join(
                    _db.LeadSyncSourceCredentials.Where(cr =>
                        cr.PullApiUrl != null && cr.PullApiUrl != ""
                        && cr.ApiKeyEncrypted != null && cr.ApiKeyEncrypted != ""),
                    x => x.Id,
                    cr => cr.SourceId,
                    (x, cr) => x.SourceId)
                .ToListAsync(cancellationToken);
        }

        public Task<LeadSyncExecutionResult> ExecuteAutoSyncAsync(
            int sourceId,
            CancellationToken cancellationToken = default) =>
            ExecuteSyncAsync(sourceId, LeadSyncType.Auto, null, cancellationToken);

        public Task<LeadSyncExecutionResult> ExecuteManualSyncAsync(
            int sourceId,
            int userId,
            CancellationToken cancellationToken = default) =>
            ExecuteSyncAsync(sourceId, LeadSyncType.Manual, userId, cancellationToken);

        public async Task<LeadSyncExecutionResult> TestConnectionAsync(
            int sourceId,
            CancellationToken cancellationToken = default)
        {
            var source = await _db.LeadSyncSources.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sourceId && s.IsActive, cancellationToken);
            if (source == null)
            {
                return Failed("Source not found.");
            }

            var resolved = await _credentials.ResolveAsync(sourceId, cancellationToken);
            if (resolved == null)
            {
                return Failed("API connection is not configured. Add the pull URL and API key first.");
            }

            var provider = _providers.FirstOrDefault(p =>
                string.Equals(p.SourceCode, source.Code, StringComparison.OrdinalIgnoreCase));
            if (provider == null || !provider.IsConfigured(resolved))
            {
                return Failed("No integration handler is available for this lead source.");
            }

            try
            {
                var pull = await provider.PullLeadsAsync(resolved, cancellationToken);
                if (!string.IsNullOrWhiteSpace(pull.ErrorMessage))
                {
                    return new LeadSyncExecutionResult
                    {
                        TotalReceived = pull.Leads.Count,
                        ErrorMessage = pull.ErrorMessage,
                        Status = LeadSyncStatus.Failed,
                        FailedCount = 1,
                    };
                }

                return new LeadSyncExecutionResult
                {
                    TotalReceived = pull.Leads.Count,
                    TotalCreated = 0,
                    Status = LeadSyncStatus.Completed,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lead sync test failed for source {SourceId}", sourceId);
                return Failed(ex.Message);
            }
        }

        private async Task<LeadSyncExecutionResult> ExecuteSyncAsync(
            int sourceId,
            LeadSyncType syncType,
            int? triggeredByUserId,
            CancellationToken cancellationToken)
        {
            var source = await _db.LeadSyncSources.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sourceId && s.IsActive, cancellationToken);
            if (source == null)
            {
                return Failed("Source not found.");
            }

            var resolved = await _credentials.ResolveAsync(sourceId, cancellationToken);
            if (resolved == null)
            {
                return Failed("API connection is not configured. Add the pull URL and API key in Advanced Settings → Lead Sync.");
            }

            var provider = _providers.FirstOrDefault(p =>
                string.Equals(p.SourceCode, source.Code, StringComparison.OrdinalIgnoreCase));
            if (provider == null || !provider.IsConfigured(resolved))
            {
                return Failed("No integration handler is available for this lead source.");
            }

            var startedAt = DateTime.UtcNow;
            var log = new LeadSyncLog
            {
                SourceId = sourceId,
                SyncType = syncType,
                StartedAt = startedAt,
                Status = LeadSyncStatus.Running,
                TriggeredByUserId = triggeredByUserId,
                CreatedAt = startedAt,
            };
            _db.LeadSyncLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);

            LeadSyncExecutionResult result;
            try
            {
                var pull = await provider.PullLeadsAsync(resolved, cancellationToken);
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
                _logger.LogError(ex, "Lead sync failed for source {SourceId}", sourceId);
                result = Failed(ex.Message);
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

        private static LeadSyncExecutionResult Failed(string message) =>
            new() { ErrorMessage = message, Status = LeadSyncStatus.Failed, FailedCount = 1 };

        private async Task<LeadSyncExecutionResult> PersistIncomingLeadsAsync(
            LeadSyncSource source,
            IReadOnlyList<LeadSyncIncomingLead> incoming,
            CancellationToken cancellationToken)
        {
            // Pull sync keeps LeadSource = "Website" for existing dashboard/filter compatibility.
            // Identity for dedupe/RR remains [crm-ext:{MarkerName}:{ExternalKey}] in Notes.
            var batch = await _marketplacePersistence.PersistAsync(
                source.MarkerName,
                "Website",
                incoming,
                cancellationToken);

            return new LeadSyncExecutionResult
            {
                TotalReceived = batch.TotalReceived,
                TotalCreated = batch.TotalCreated,
                FailedCount = batch.FailedCount,
                ErrorMessage = batch.ErrorMessage,
                Status = batch.Status,
            };
        }

        private async Task UpdateSyncTimestampsAsync(
            int sourceId,
            DateTime endedAt,
            CancellationToken cancellationToken)
        {
            var config = await _db.LeadSyncSourceConfigs
                .FirstOrDefaultAsync(c => c.SourceId == sourceId, cancellationToken);
            if (config == null)
            {
                return;
            }

            config.LastSyncAt = endedAt;
            config.NextSyncAt = config.AutoSyncEnabled ? endedAt : null;
            config.UpdatedAt = endedAt;
        }
    }

    public class LeadSyncIndiaMartProvider : ILeadSyncProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LeadSyncIndiaMartProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public string SourceCode => "indiamart";

        public bool IsConfigured(LeadSyncResolvedCredentials credentials) =>
            !string.IsNullOrWhiteSpace(credentials.PullApiUrl)
            && !string.IsNullOrWhiteSpace(credentials.ApiKey);

        public async Task<LeadSyncPullResult> PullLeadsAsync(
            LeadSyncResolvedCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var url = LeadSyncPullHelpers.BuildIndiaMartPullUrl(credentials);
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

            var err = LeadSyncPullHelpers.TryGetIndiaMartError(body);
            if (err != null)
            {
                return new LeadSyncPullResult { ErrorMessage = err };
            }

            var leads = LeadSyncPullHelpers.ExtractLeadArray(body)
                .Select(row => LeadSyncPullHelpers.MapGenericMarketplaceRow(row, "IndiaMART", "IndiaMART"))
                .Where(l => l != null)
                .Cast<LeadSyncIncomingLead>()
                .ToList();

            return new LeadSyncPullResult { Leads = leads };
        }
    }

    public class LeadSyncTradeIndiaProvider : ILeadSyncProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LeadSyncTradeIndiaProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public string SourceCode => "tradeindia";

        public bool IsConfigured(LeadSyncResolvedCredentials credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.ApiKey))
            {
                return false;
            }

            var sanitized = LeadSyncPullHelpers.SanitizeTradeIndiaPullUrl(
                credentials.PullApiUrl,
                out _,
                out _);
            return sanitized != null;
        }

        public async Task<LeadSyncPullResult> PullLeadsAsync(
            LeadSyncResolvedCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured(credentials))
            {
                return new LeadSyncPullResult
                {
                    ErrorMessage =
                        "TradeIndia URL must be https://www.tradeindia.com/... with userid and profile_id. "
                        + "Save the API key in the API key field (not in the URL).",
                };
            }

            string url;
            try
            {
                url = LeadSyncPullHelpers.BuildTradeIndiaPullUrl(credentials);
            }
            catch (Exception ex)
            {
                return new LeadSyncPullResult { ErrorMessage = ex.Message };
            }

            var client = _httpClientFactory.CreateClient("LeadSyncMarketplace");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                return new LeadSyncPullResult { ErrorMessage = ex.Message };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new LeadSyncPullResult
                {
                    ErrorMessage = LeadSyncPullHelpers.FormatMarketplaceHttpError(
                        "TradeIndia",
                        response.StatusCode),
                };
            }

            string raw;
            try
            {
                raw = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return new LeadSyncPullResult { ErrorMessage = ex.Message };
            }

            if (!LeadSyncPullHelpers.TryParseJsonElement(raw, out var body, out var parseError))
            {
                return new LeadSyncPullResult { ErrorMessage = parseError };
            }

            var leads = LeadSyncPullHelpers.ExtractLeadArray(body)
                .Select(row => LeadSyncPullHelpers.MapGenericMarketplaceRow(row, "TradeIndia", "TradeIndia"))
                .Where(l => l != null)
                .Cast<LeadSyncIncomingLead>()
                .ToList();

            return new LeadSyncPullResult { Leads = leads };
        }
    }

    public class LeadSyncJustdialProvider : ILeadSyncProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LeadSyncJustdialProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public string SourceCode => "justdial";

        public bool IsConfigured(LeadSyncResolvedCredentials credentials) =>
            !string.IsNullOrWhiteSpace(credentials.PullApiUrl)
            && !string.IsNullOrWhiteSpace(credentials.ApiKey);

        public async Task<LeadSyncPullResult> PullLeadsAsync(
            LeadSyncResolvedCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var url = LeadSyncPullHelpers.BuildBearerPullUrl(credentials);
            var client = _httpClientFactory.CreateClient("LeadSyncMarketplace");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.ApiKey.Trim());

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                return new LeadSyncPullResult { ErrorMessage = ex.Message };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new LeadSyncPullResult
                {
                    ErrorMessage = LeadSyncPullHelpers.FormatMarketplaceHttpError(
                        "Justdial",
                        response.StatusCode),
                };
            }

            JsonElement body;
            try
            {
                body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            }
            catch (Exception ex)
            {
                return new LeadSyncPullResult { ErrorMessage = ex.Message };
            }

            var leads = LeadSyncPullHelpers.ExtractLeadArray(body)
                .Select(row => LeadSyncPullHelpers.MapGenericMarketplaceRow(row, "Justdial", "Justdial"))
                .Where(l => l != null)
                .Cast<LeadSyncIncomingLead>()
                .ToList();

            return new LeadSyncPullResult { Leads = leads };
        }
    }
}
