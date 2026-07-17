using System.Diagnostics;
using System.Text.Json;
using CRM.Configuration;
using CRM.DTO;
using CRM.Helpers;
using Microsoft.Extensions.Options;

namespace CRM.Services
{
    public sealed class JustdialWebhookRequestContext
    {
        public required string CorrelationId { get; init; }
        public string? RemoteIp { get; init; }
        public string? UserAgent { get; init; }
        public required string HttpMethod { get; init; }
        public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    }

    public interface IJustdialWebhookService
    {
        Task ProcessAsync(
            JustdialWebhookLeadDto? dto,
            JustdialWebhookRequestContext requestContext,
            CancellationToken cancellationToken = default);
    }

    public sealed class JustdialWebhookService : IJustdialWebhookService
    {
        private readonly IMarketplaceLeadPersistenceService _marketplacePersistence;
        private readonly IJustdialWebhookMetrics _metrics;
        private readonly JustdialWebhookOptions _options;
        private readonly ILogger<JustdialWebhookService> _logger;

        public JustdialWebhookService(
            IMarketplaceLeadPersistenceService marketplacePersistence,
            IJustdialWebhookMetrics metrics,
            IOptions<JustdialWebhookOptions> options,
            ILogger<JustdialWebhookService> logger)
        {
            _marketplacePersistence = marketplacePersistence;
            _metrics = metrics;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ProcessAsync(
            JustdialWebhookLeadDto? dto,
            JustdialWebhookRequestContext requestContext,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var correlationId = requestContext.CorrelationId;

            _metrics.IncrementReceived();

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Integration"] = "JustdialWebhook"
            }))
            {
                _logger.LogInformation(
                    "Request received. CorrelationId={CorrelationId} TimestampUtc={TimestampUtc} RemoteIp={RemoteIp} UserAgent={UserAgent} HttpMethod={HttpMethod}",
                    correlationId,
                    requestContext.TimestampUtc,
                    requestContext.RemoteIp,
                    requestContext.UserAgent,
                    requestContext.HttpMethod);

                try
                {
                    if (dto == null)
                    {
                        _metrics.IncrementValidationFailed();
                        _logger.LogWarning(
                            "Lead skipped. CorrelationId={CorrelationId} Reason=ValidationFailed Detail=payload_null",
                            correlationId);
                        return;
                    }

                    if (_options.EnableDetailedPayloadLogging)
                    {
                        _logger.LogInformation(
                            "Payload snapshot. CorrelationId={CorrelationId} Payload={Payload}",
                            correlationId,
                            BuildRedactedPayloadLog(dto));
                    }

                    var errors = Validate(dto);
                    if (errors.Count > 0)
                    {
                        _metrics.IncrementValidationFailed();
                        _logger.LogWarning(
                            "Validation failed. CorrelationId={CorrelationId} Errors={Errors}",
                            correlationId,
                            string.Join("; ", errors));
                        _logger.LogInformation(
                            "Lead skipped. CorrelationId={CorrelationId} Reason=ValidationFailed",
                            correlationId);
                        return;
                    }

                    _logger.LogInformation(
                        "Validation success. CorrelationId={CorrelationId} LeadId={LeadId}",
                        correlationId,
                        dto.Leadid);

                    var incoming = MarketplaceLeadMapper.FromJustdial(dto);
                    _logger.LogInformation(
                        "Mapping completed. CorrelationId={CorrelationId} ExternalKey={ExternalKey}",
                        correlationId,
                        incoming.ExternalKey);

                    using (await JustdialWebhookLeadLocks.AcquireAsync(incoming.ExternalKey, cancellationToken))
                    {
                        var result = await _marketplacePersistence.PersistOneAsync(
                            MarketplaceLeadMapper.JustdialMarkerName,
                            MarketplaceLeadMapper.JustdialLeadSource,
                            incoming,
                            cancellationToken);

                        LogPersistOutcome(correlationId, result);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _metrics.IncrementPersistenceFailed();
                    _logger.LogWarning(
                        "Processing cancelled (timeout or client abort). CorrelationId={CorrelationId}",
                        correlationId);
                }
                catch (Exception ex)
                {
                    _metrics.IncrementPersistenceFailed();
                    _logger.LogError(
                        ex,
                        "Unexpected exception while processing Justdial webhook. CorrelationId={CorrelationId}",
                        correlationId);
                }
                finally
                {
                    sw.Stop();
                    _metrics.RecordProcessingTime(sw.ElapsedMilliseconds);
                    _logger.LogInformation(
                        "Processing time. CorrelationId={CorrelationId} ElapsedMs={ElapsedMs}",
                        correlationId,
                        sw.ElapsedMilliseconds);
                }
            }
        }

        private void LogPersistOutcome(string correlationId, MarketplaceLeadPersistItemResult result)
        {
            switch (result.Outcome)
            {
                case MarketplaceLeadPersistOutcome.Duplicate:
                    _metrics.IncrementDuplicates();
                    _logger.LogInformation(
                        "Duplicate detected. CorrelationId={CorrelationId} ExternalKey={ExternalKey}",
                        correlationId,
                        result.ExternalKey);
                    _logger.LogInformation(
                        "Lead skipped. CorrelationId={CorrelationId} Reason=Duplicate",
                        correlationId);
                    break;

                case MarketplaceLeadPersistOutcome.Created:
                    _metrics.IncrementInserted();
                    _logger.LogInformation(
                        "Lead inserted. CorrelationId={CorrelationId} LeadId={LeadId} LeadSource={LeadSource}",
                        correlationId,
                        result.LeadId,
                        MarketplaceLeadMapper.JustdialLeadSource);

                    if (result.RoundRobinAssigned)
                    {
                        _logger.LogInformation(
                            "Round Robin assigned. CorrelationId={CorrelationId} LeadId={LeadId} OwnerId={OwnerId}",
                            correlationId,
                            result.LeadId,
                            result.LeadOwnerId);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Round Robin not applied. CorrelationId={CorrelationId} LeadId={LeadId}",
                            correlationId,
                            result.LeadId);
                    }

                    _logger.LogInformation(
                        result.ContactCreated
                            ? "Contact created. CorrelationId={CorrelationId} LeadId={LeadId}"
                            : "Contact not created. CorrelationId={CorrelationId} LeadId={LeadId}",
                        correlationId,
                        result.LeadId);

                    _logger.LogInformation(
                        "Activity created via SaveChanges/ActivityCapture. CorrelationId={CorrelationId} LeadId={LeadId}",
                        correlationId,
                        result.LeadId);
                    break;

                case MarketplaceLeadPersistOutcome.ValidationFailed:
                    _metrics.IncrementValidationFailed();
                    _logger.LogWarning(
                        "Marketplace validation failed. CorrelationId={CorrelationId} ExternalKey={ExternalKey} Error={Error}",
                        correlationId,
                        result.ExternalKey,
                        result.ErrorMessage);
                    _logger.LogInformation(
                        "Lead skipped. CorrelationId={CorrelationId} Reason=MarketplaceValidationFailed",
                        correlationId);
                    break;

                default:
                    _metrics.IncrementPersistenceFailed();
                    _logger.LogError(
                        "Database failure while inserting Justdial lead. CorrelationId={CorrelationId} ExternalKey={ExternalKey} Error={Error}",
                        correlationId,
                        result.ExternalKey,
                        result.ErrorMessage);
                    break;
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

        private static string BuildRedactedPayloadLog(JustdialWebhookLeadDto dto)
        {
            var snapshot = new
            {
                leadid = dto.Leadid,
                leadtype = dto.Leadtype,
                prefix = dto.Prefix,
                name = dto.Name,
                mobile = MaskSecret(dto.Mobile),
                phone = MaskSecret(dto.Phone),
                email = MaskEmail(dto.Email),
                date = dto.Date,
                category = dto.Category,
                city = dto.City,
                area = dto.Area,
                company = dto.Company,
                pincode = dto.Pincode,
                time = dto.Time,
                parentid = dto.Parentid
            };

            return JsonSerializer.Serialize(snapshot);
        }

        private static string? MaskSecret(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var trimmed = value.Trim();
            if (trimmed.Length <= 4)
            {
                return "****";
            }

            return new string('*', trimmed.Length - 4) + trimmed[^4..];
        }

        private static string? MaskEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var trimmed = value.Trim();
            var at = trimmed.IndexOf('@');
            if (at <= 1)
            {
                return "***";
            }

            return trimmed[0] + "***" + trimmed[at..];
        }
    }
}
