using System.Diagnostics;
using System.Text.Json;
using CRM.Configuration;
using CRM.DTO;
using CRM.Helpers;
using Microsoft.Extensions.Options;

namespace CRM.Services
{
    public sealed class IndiaMartWebhookRequestContext
    {
        public required string CorrelationId { get; init; }
        public string? RemoteIp { get; init; }
        public string? UserAgent { get; init; }
        public required string HttpMethod { get; init; }
        public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    }

    public enum IndiaMartWebhookProcessingOutcome
    {
        Success,
        Duplicate,
        ValidationFailed,
        PersistenceFailed
    }

    public sealed class IndiaMartWebhookProcessingResult
    {
        public IndiaMartWebhookProcessingOutcome Outcome { get; init; }
        public string ExternalKey { get; init; } = string.Empty;
        public int? LeadId { get; init; }
        public int? LeadOwnerId { get; init; }
        public bool IsDuplicate => Outcome == IndiaMartWebhookProcessingOutcome.Duplicate;
        public bool RoundRobinAssigned { get; init; }
        public bool ContactCreated { get; init; }
        public string? Message { get; init; }
    }

    public interface IIndiaMartWebhookService
    {
        Task<IndiaMartWebhookProcessingResult> ProcessAsync(
            IndiaMartWebhookLeadDto? dto,
            IndiaMartWebhookRequestContext requestContext,
            CancellationToken cancellationToken = default);
    }

    public sealed class IndiaMartWebhookService : IIndiaMartWebhookService
    {
        private readonly IMarketplaceLeadPersistenceService _marketplacePersistence;
        private readonly IIndiaMartWebhookMetrics _metrics;
        private readonly IndiaMartWebhookOptions _options;
        private readonly ILogger<IndiaMartWebhookService> _logger;

        public IndiaMartWebhookService(
            IMarketplaceLeadPersistenceService marketplacePersistence,
            IIndiaMartWebhookMetrics metrics,
            IOptions<IndiaMartWebhookOptions> options,
            ILogger<IndiaMartWebhookService> logger)
        {
            _marketplacePersistence = marketplacePersistence;
            _metrics = metrics;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IndiaMartWebhookProcessingResult> ProcessAsync(
            IndiaMartWebhookLeadDto? dto,
            IndiaMartWebhookRequestContext requestContext,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var correlationId = requestContext.CorrelationId;

            _metrics.IncrementReceived();

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Integration"] = "IndiaMartWebhook"
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
                        _metrics.RecordFailure();
                        _logger.LogWarning(
                            "Lead skipped. CorrelationId={CorrelationId} Reason=ValidationFailed Detail=payload_null",
                            correlationId);

                        return new IndiaMartWebhookProcessingResult
                        {
                            Outcome = IndiaMartWebhookProcessingOutcome.ValidationFailed,
                            Message = "Payload is null."
                        };
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
                        _metrics.RecordFailure();
                        _logger.LogWarning(
                            "Validation failed. CorrelationId={CorrelationId} Errors={Errors}",
                            correlationId,
                            string.Join("; ", errors));

                        return new IndiaMartWebhookProcessingResult
                        {
                            Outcome = IndiaMartWebhookProcessingOutcome.ValidationFailed,
                            ExternalKey = dto.GetEffectiveExternalKey(),
                            Message = string.Join("; ", errors)
                        };
                    }

                    _logger.LogInformation(
                        "Validation success. CorrelationId={CorrelationId} ExternalKey={ExternalKey}",
                        correlationId,
                        dto.GetEffectiveExternalKey());

                    var incoming = MarketplaceLeadMapper.FromIndiaMartPush(dto);

                    using (await IndiaMartWebhookLeadLocks.AcquireAsync(incoming.ExternalKey, cancellationToken))
                    {
                        var persistResult = await _marketplacePersistence.PersistOneAsync(
                            MarketplaceLeadMapper.IndiaMartMarkerName,
                            MarketplaceLeadMapper.IndiaMartLeadSource,
                            incoming,
                            cancellationToken);

                        return MapAndLogPersistOutcome(correlationId, incoming.ExternalKey, persistResult);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _metrics.IncrementPersistenceFailed();
                    _metrics.RecordFailure();
                    _logger.LogWarning(
                        "Processing cancelled (timeout or client abort). CorrelationId={CorrelationId}",
                        correlationId);

                    return new IndiaMartWebhookProcessingResult
                    {
                        Outcome = IndiaMartWebhookProcessingOutcome.PersistenceFailed,
                        Message = "Processing cancelled or timed out."
                    };
                }
                catch (Exception ex)
                {
                    _metrics.IncrementPersistenceFailed();
                    _metrics.RecordFailure();
                    _logger.LogError(
                        ex,
                        "Unexpected exception while processing IndiaMART webhook. CorrelationId={CorrelationId}",
                        correlationId);

                    return new IndiaMartWebhookProcessingResult
                    {
                        Outcome = IndiaMartWebhookProcessingOutcome.PersistenceFailed,
                        Message = "Unexpected server error."
                    };
                }
                finally
                {
                    sw.Stop();
                    _metrics.RecordProcessingTime(sw.ElapsedMilliseconds);
                    _logger.LogInformation(
                        "Request completed. CorrelationId={CorrelationId} ElapsedMs={ElapsedMs}",
                        correlationId,
                        sw.ElapsedMilliseconds);
                }
            }
        }

        private IndiaMartWebhookProcessingResult MapAndLogPersistOutcome(
            string correlationId,
            string externalKey,
            MarketplaceLeadPersistItemResult persistResult)
        {
            switch (persistResult.Outcome)
            {
                case MarketplaceLeadPersistOutcome.Created:
                    _metrics.IncrementInserted();
                    _metrics.RecordSuccess();
                    _logger.LogInformation(
                        "Lead persisted. CorrelationId={CorrelationId} LeadId={LeadId} LeadOwnerId={LeadOwnerId} RoundRobinAssigned={RoundRobinAssigned} ContactCreated={ContactCreated} ExternalKey={ExternalKey}",
                        correlationId,
                        persistResult.LeadId,
                        persistResult.LeadOwnerId,
                        persistResult.RoundRobinAssigned,
                        persistResult.ContactCreated,
                        externalKey);

                    return new IndiaMartWebhookProcessingResult
                    {
                        Outcome = IndiaMartWebhookProcessingOutcome.Success,
                        ExternalKey = externalKey,
                        LeadId = persistResult.LeadId,
                        LeadOwnerId = persistResult.LeadOwnerId,
                        RoundRobinAssigned = persistResult.RoundRobinAssigned,
                        ContactCreated = persistResult.ContactCreated,
                        Message = "Lead created successfully."
                    };

                case MarketplaceLeadPersistOutcome.Duplicate:
                    _metrics.IncrementDuplicates();
                    _metrics.RecordSuccess();
                    _logger.LogInformation(
                        "Lead duplicate ignored. CorrelationId={CorrelationId} ExternalKey={ExternalKey}",
                        correlationId,
                        externalKey);

                    return new IndiaMartWebhookProcessingResult
                    {
                        Outcome = IndiaMartWebhookProcessingOutcome.Duplicate,
                        ExternalKey = externalKey,
                        Message = "Duplicate IndiaMART lead."
                    };

                case MarketplaceLeadPersistOutcome.ValidationFailed:
                    _metrics.IncrementValidationFailed();
                    _metrics.RecordFailure();
                    _logger.LogWarning(
                        "Persistence rejected validation. CorrelationId={CorrelationId} ExternalKey={ExternalKey} Error={Error}",
                        correlationId,
                        externalKey,
                        persistResult.ErrorMessage);

                    return new IndiaMartWebhookProcessingResult
                    {
                        Outcome = IndiaMartWebhookProcessingOutcome.ValidationFailed,
                        ExternalKey = externalKey,
                        Message = persistResult.ErrorMessage
                    };

                default:
                    _metrics.IncrementPersistenceFailed();
                    _metrics.RecordFailure();
                    _logger.LogError(
                        "Persistence failed. CorrelationId={CorrelationId} ExternalKey={ExternalKey} Error={Error}",
                        correlationId,
                        externalKey,
                        persistResult.ErrorMessage);

                    return new IndiaMartWebhookProcessingResult
                    {
                        Outcome = IndiaMartWebhookProcessingOutcome.PersistenceFailed,
                        ExternalKey = externalKey,
                        Message = persistResult.ErrorMessage ?? "Persistence failed."
                    };
            }
        }

        private static List<string> Validate(IndiaMartWebhookLeadDto dto)
        {
            var errors = new List<string>(3);

            var extKey = dto.GetEffectiveExternalKey();
            if (string.IsNullOrWhiteSpace(extKey))
            {
                errors.Add("Missing UNIQUE_QUERY_ID or natural identifier.");
            }

            var hasName = !string.IsNullOrWhiteSpace(dto.SenderName);
            var hasMobile = !string.IsNullOrWhiteSpace(dto.SenderMobile);
            var hasEmail = !string.IsNullOrWhiteSpace(dto.SenderEmail);

            if (!hasName && !hasMobile && !hasEmail)
            {
                errors.Add("Missing sender contact information (name, mobile, or email).");
            }
            else if (!hasMobile && !hasEmail)
            {
                errors.Add("Either SENDER_MOBILE or SENDER_EMAIL is required.");
            }

            return errors;
        }

        private static string BuildRedactedPayloadLog(IndiaMartWebhookLeadDto dto)
        {
            var redacted = new Dictionary<string, object?>
            {
                ["UNIQUE_QUERY_ID"] = dto.GetEffectiveExternalKey(),
                ["SENDER_NAME"] = dto.SenderName,
                ["SENDER_MOBILE"] = RedactMobile(dto.SenderMobile),
                ["SENDER_EMAIL"] = RedactEmail(dto.SenderEmail),
                ["SUBJECT"] = dto.Subject,
                ["QUERY_PRODUCT_NAME"] = dto.QueryProductName,
                ["GLUSR_USR_COMPANYNAME"] = dto.GlusrUsrCompanyName,
                ["SENDER_CITY"] = dto.SenderCity,
                ["QUERY_TIME"] = dto.QueryTime,
                ["GLUSR_CRM_KEY"] = !string.IsNullOrWhiteSpace(dto.GlusrCrmKey) ? "[REDACTED]" : null
            };

            return JsonSerializer.Serialize(redacted);
        }

        private static string? RedactMobile(string? mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile)) return null;
            var trimmed = mobile.Trim();
            return trimmed.Length <= 4 ? "****" : $"{new string('*', trimmed.Length - 4)}{trimmed[^4..]}";
        }

        private static string? RedactEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1) return "***@***";
            return $"{email[0]}***{email[atIndex..]}";
        }
    }
}
