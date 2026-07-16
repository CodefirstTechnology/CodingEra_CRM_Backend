using System.Diagnostics;
using System.Text.Json;
using CRM.DTO;
using CRM.Helpers;

namespace CRM.Services
{
    public interface IJustdialWebhookService
    {
        Task ProcessAsync(JustdialWebhookLeadDto? dto, CancellationToken cancellationToken = default);
    }

    public sealed class JustdialWebhookService : IJustdialWebhookService
    {
        private static readonly JsonSerializerOptions PayloadLogJsonOptions = new()
        {
            WriteIndented = false
        };

        private readonly IMarketplaceLeadPersistenceService _marketplacePersistence;
        private readonly ILogger<JustdialWebhookService> _logger;

        public JustdialWebhookService(
            IMarketplaceLeadPersistenceService marketplacePersistence,
            ILogger<JustdialWebhookService> logger)
        {
            _marketplacePersistence = marketplacePersistence;
            _logger = logger;
        }

        public async Task ProcessAsync(JustdialWebhookLeadDto? dto, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Webhook received");

            try
            {
                if (dto == null)
                {
                    _logger.LogWarning("Validation failed: payload is null or empty.");
                    return;
                }

                var payloadJson = JsonSerializer.Serialize(dto, PayloadLogJsonOptions);
                _logger.LogInformation("Payload: {Payload}", payloadJson);

                var errors = Validate(dto);
                if (errors.Count > 0)
                {
                    _logger.LogWarning("Validation failed: {Errors}", string.Join("; ", errors));
                    return;
                }

                _logger.LogInformation("Validation passed. leadid={LeadId}", dto.Leadid);

                var incoming = MarketplaceLeadMapper.FromJustdial(dto);
                _logger.LogInformation(
                    "Mapping completed. ExternalKey={ExternalKey} MarkerNotes={Notes}",
                    incoming.ExternalKey,
                    incoming.Notes);

                var result = await _marketplacePersistence.PersistOneAsync(
                    MarketplaceLeadMapper.JustdialMarkerName,
                    MarketplaceLeadMapper.JustdialLeadSource,
                    incoming,
                    cancellationToken);

                switch (result.Outcome)
                {
                    case MarketplaceLeadPersistOutcome.Duplicate:
                        _logger.LogInformation(
                            "Duplicate detected. ExternalKey={ExternalKey}. Skipping insert.",
                            result.ExternalKey);
                        break;

                    case MarketplaceLeadPersistOutcome.Created:
                        _logger.LogInformation(
                            "Lead inserted. LeadId={LeadId} LeadSource={LeadSource}",
                            result.LeadId,
                            MarketplaceLeadMapper.JustdialLeadSource);

                        if (result.RoundRobinAssigned)
                        {
                            _logger.LogInformation(
                                "Round Robin assigned. LeadId={LeadId} OwnerId={OwnerId}",
                                result.LeadId,
                                result.LeadOwnerId);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Round Robin not applied (no active assignments or source marker). LeadId={LeadId}",
                                result.LeadId);
                        }

                        if (result.ContactCreated)
                        {
                            _logger.LogInformation(
                                "Contact created for LeadId={LeadId}",
                                result.LeadId);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Contact not created (skipped or duplicate contact). LeadId={LeadId}",
                                result.LeadId);
                        }

                        _logger.LogInformation(
                            "Activity created via SaveChanges/ActivityCapture. LeadId={LeadId}",
                            result.LeadId);
                        break;

                    case MarketplaceLeadPersistOutcome.ValidationFailed:
                        _logger.LogWarning(
                            "Marketplace validation failed. ExternalKey={ExternalKey} Error={Error}",
                            result.ExternalKey,
                            result.ErrorMessage);
                        break;

                    default:
                        _logger.LogError(
                            "Database error while inserting Justdial lead. ExternalKey={ExternalKey} Error={Error}",
                            result.ExternalKey,
                            result.ErrorMessage);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception while processing Justdial webhook.");
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Justdial webhook execution time: {ElapsedMs} ms", sw.ElapsedMilliseconds);
            }
        }

        private static List<string> Validate(JustdialWebhookLeadDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.Leadid))
            {
                errors.Add("leadid is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                errors.Add("name is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Mobile))
            {
                errors.Add("mobile is required.");
            }

            return errors;
        }
    }
}
