using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface ILeadSyncCredentialService
    {
        bool IsConfigured(LeadSyncSourceCredentials? row);
        Task<LeadSyncResolvedCredentials?> ResolveAsync(int sourceId, CancellationToken cancellationToken = default);
        Task<LeadSyncCredentialsDto> GetMaskedAsync(int sourceId, CancellationToken cancellationToken = default);
        Task<LeadSyncCredentialsDto> SaveAsync(
            int sourceId,
            LeadSyncSaveCredentialsDto dto,
            int actingUserId,
            CancellationToken cancellationToken = default);
        Task ClearAsync(int sourceId, int actingUserId, CancellationToken cancellationToken = default);
        Task RefreshIntegrationReadyAsync(int sourceId, CancellationToken cancellationToken = default);
    }

    public class LeadSyncCredentialService : ILeadSyncCredentialService
    {
        private const string ProtectorPurpose = "CRM.LeadSync.ApiKey.v1";
        private readonly TaskDbcontext _db;
        private readonly IDataProtector _protector;

        public LeadSyncCredentialService(TaskDbcontext db, IDataProtectionProvider dataProtection)
        {
            _db = db;
            _protector = dataProtection.CreateProtector(ProtectorPurpose);
        }

        public bool IsConfigured(LeadSyncSourceCredentials? row)
        {
            return row != null
                && !string.IsNullOrWhiteSpace(row.PullApiUrl)
                && !string.IsNullOrWhiteSpace(row.ApiKeyEncrypted);
        }

        public async Task<LeadSyncResolvedCredentials?> ResolveAsync(
            int sourceId,
            CancellationToken cancellationToken = default)
        {
            var row = await _db.LeadSyncSourceCredentials.AsNoTracking()
                .FirstOrDefaultAsync(c => c.SourceId == sourceId, cancellationToken);
            if (!IsConfigured(row))
            {
                return null;
            }

            try
            {
                var apiKey = _protector.Unprotect(row!.ApiKeyEncrypted!);
                return new LeadSyncResolvedCredentials
                {
                    PullApiUrl = row.PullApiUrl!.Trim(),
                    ApiKey = apiKey,
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<LeadSyncCredentialsDto> GetMaskedAsync(
            int sourceId,
            CancellationToken cancellationToken = default)
        {
            var source = await _db.LeadSyncSources.AsNoTracking()
                .Include(s => s.Credentials)
                .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);
            var row = source?.Credentials;
            var pullUrl = row?.PullApiUrl;

            if (string.Equals(source?.Code, "tradeindia", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(pullUrl))
            {
                var sanitized = LeadSyncPullHelpers.SanitizeTradeIndiaPullUrl(pullUrl, out _, out _);
                if (sanitized != null)
                {
                    pullUrl = sanitized;
                }
            }

            return new LeadSyncCredentialsDto
            {
                PullApiUrl = pullUrl,
                HasApiKey = IsConfigured(row),
                ApiKeyMasked = MaskApiKey(row),
                ConfiguredAt = row?.ConfiguredAt,
            };
        }

        public async Task<LeadSyncCredentialsDto> SaveAsync(
            int sourceId,
            LeadSyncSaveCredentialsDto dto,
            int actingUserId,
            CancellationToken cancellationToken = default)
        {
            var source = await _db.LeadSyncSources
                .Include(s => s.Credentials)
                .FirstOrDefaultAsync(s => s.Id == sourceId && s.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Source not found.");

            var row = source.Credentials;
            if (row == null)
            {
                row = new LeadSyncSourceCredentials { SourceId = sourceId };
                _db.LeadSyncSourceCredentials.Add(row);
                source.Credentials = row;
            }

            var pullUrl = dto.PullApiUrl?.Trim();
            string? keyFromUrl = null;
            var isTradeIndia = string.Equals(source.Code, "tradeindia", StringComparison.OrdinalIgnoreCase);

            if (isTradeIndia)
            {
                var sourceUrl = !string.IsNullOrWhiteSpace(pullUrl) ? pullUrl : row.PullApiUrl;
                var sanitized = LeadSyncPullHelpers.SanitizeTradeIndiaPullUrl(
                    sourceUrl,
                    out keyFromUrl,
                    out var urlError);
                if (sanitized == null)
                {
                    throw new InvalidOperationException(urlError ?? "Invalid TradeIndia pull URL.");
                }

                row.PullApiUrl = sanitized;
            }
            else if (!string.IsNullOrWhiteSpace(pullUrl))
            {
                row.PullApiUrl = pullUrl;
            }
            else if (string.IsNullOrWhiteSpace(row.PullApiUrl))
            {
                throw new InvalidOperationException("Lead pull URL is required.");
            }

            var apiKey = dto.ApiKey?.Trim();
            if (string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(keyFromUrl))
            {
                // If admin pasted key into the URL, move it into encrypted storage.
                apiKey = keyFromUrl;
            }

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                row.ApiKeyEncrypted = _protector.Protect(apiKey);
            }
            else if (string.IsNullOrWhiteSpace(row.ApiKeyEncrypted))
            {
                throw new InvalidOperationException("API key is required.");
            }

            var now = DateTime.UtcNow;
            row.ConfiguredAt = now;
            row.ConfiguredBy = actingUserId;
            row.UpdatedAt = now;
            source.ApiIntegrationReady = true;
            source.UpdatedAt = now;

            await _db.SaveChangesAsync(cancellationToken);

            return await GetMaskedAsync(sourceId, cancellationToken);
        }

        public async Task ClearAsync(
            int sourceId,
            int actingUserId,
            CancellationToken cancellationToken = default)
        {
            var source = await _db.LeadSyncSources
                .Include(s => s.Credentials)
                .Include(s => s.Config)
                .Include(s => s.Assignments)
                .FirstOrDefaultAsync(s => s.Id == sourceId && s.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Source not found.");

            var now = DateTime.UtcNow;

            if (source.Credentials != null)
            {
                source.Credentials.PullApiUrl = null;
                source.Credentials.ApiKeyEncrypted = null;
                source.Credentials.ConfiguredAt = null;
                source.Credentials.ConfiguredBy = null;
                source.Credentials.UpdatedAt = now;
            }

            if (source.Config != null)
            {
                source.Config.AutoSyncEnabled = false;
                source.Config.IntervalOptionId = null;
                source.Config.NextSyncAt = null;
                source.Config.UpdatedAt = now;
                source.Config.UpdatedBy = actingUserId;
            }

            if (source.Assignments.Count > 0)
            {
                _db.LeadSyncSourceAssignments.RemoveRange(source.Assignments);
            }

            source.ApiIntegrationReady = false;
            source.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RefreshIntegrationReadyAsync(
            int sourceId,
            CancellationToken cancellationToken = default)
        {
            var source = await _db.LeadSyncSources
                .Include(s => s.Credentials)
                .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);
            if (source == null)
            {
                return;
            }

            source.ApiIntegrationReady = IsConfigured(source.Credentials);
            source.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private string? MaskApiKey(LeadSyncSourceCredentials? row)
        {
            if (!IsConfigured(row))
            {
                return null;
            }

            try
            {
                var key = _protector.Unprotect(row!.ApiKeyEncrypted!);
                if (key.Length <= 4)
                {
                    return "••••";
                }

                return $"••••••••{key[^4..]}";
            }
            catch
            {
                return "••••";
            }
        }
    }
}
