using CRM.Configuration;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CRM.Controllers
{
    /// <summary>
    /// Inbound IndiaMART Webhook / Push API controller.
    /// Handles lead notifications, health checks, metrics, and admin test facility.
    /// </summary>
    [Route("api/integrations/indiamart")]
    [ApiController]
    public class IndiaMartWebhookController : ControllerBase
    {
        private readonly IIndiaMartWebhookService _service;
        private readonly IIndiaMartWebhookSecurityService _security;
        private readonly IIndiaMartWebhookMetrics _metrics;
        private readonly IndiaMartWebhookOptions _options;
        private readonly TaskDbcontext _db;
        private readonly ILogger<IndiaMartWebhookController> _logger;

        public IndiaMartWebhookController(
            IIndiaMartWebhookService service,
            IIndiaMartWebhookSecurityService security,
            IIndiaMartWebhookMetrics metrics,
            IOptions<IndiaMartWebhookOptions> options,
            TaskDbcontext db,
            ILogger<IndiaMartWebhookController> logger)
        {
            _service = service;
            _security = security;
            _metrics = metrics;
            _options = options.Value;
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Connectivity verification check only.
        /// Does NOT create, update, or persist CRM leads.
        /// </summary>
        [HttpGet("leads")]
        [Produces("application/json")]
        public IActionResult Get()
        {
            var correlationId = ResolveCorrelationId();
            Response.Headers[_options.CorrelationIdHeaderName] = correlationId;

            return Ok(new
            {
                status = "reachable",
                provider = "IndiaMART",
                message = "IndiaMART webhook listener is reachable. Use POST /leads for lead ingestion.",
                correlationId
            });
        }

        /// <summary>
        /// Primary webhook lead receiver (JSON payload).
        /// Accepts standard IndiaMART lead notifications.
        /// </summary>
        /// <remarks>
        /// Sample request body:
        ///
        ///     {
        ///       "UNIQUE_QUERY_ID": "IM-PUSH-99001",
        ///       "SENDER_NAME": "Rajesh Sharma",
        ///       "SENDER_MOBILE": "9876543210",
        ///       "SENDER_EMAIL": "rajesh.sharma@example.com",
        ///       "SUBJECT": "Industrial Valve Inquiry",
        ///       "QUERY_PRODUCT_NAME": "Industrial Control Valve",
        ///       "QUERY_MESSAGE": "Looking for 50 units of stainless steel control valves.",
        ///       "GLUSR_USR_COMPANYNAME": "Sharma Engineering Works",
        ///       "SENDER_CITY": "Ahmedabad",
        ///       "SENDER_STATE": "Gujarat",
        ///       "QUERY_TIME": "2026-08-21 12:00:00"
        ///     }
        ///
        /// </remarks>
        [HttpPost("leads")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> PostJson(
            [FromBody] IndiaMartWebhookLeadDto? dto,
            CancellationToken cancellationToken)
        {
            if (_options.MaxRequestBodyBytes > 0
                && Request.ContentLength is long length
                && length > _options.MaxRequestBodyBytes)
            {
                return Reject(
                    IndiaMartWebhookSecurityStatus.PayloadTooLarge,
                    "Request body exceeds configured size limit.");
            }

            return await ProcessLeadAsync(dto, cancellationToken);
        }

        /// <summary>
        /// Secondary webhook receiver (form-urlencoded payload).
        /// Hidden from Swagger to avoid duplicate route documentation.
        /// </summary>
        [HttpPost("leads")]
        [Consumes("application/x-www-form-urlencoded")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Produces("application/json")]
        public async Task<IActionResult> PostForm(
            [FromForm] IndiaMartWebhookLeadDto dto,
            CancellationToken cancellationToken)
        {
            if (_options.MaxRequestBodyBytes > 0
                && Request.ContentLength is long length
                && length > _options.MaxRequestBodyBytes)
            {
                return Reject(
                    IndiaMartWebhookSecurityStatus.PayloadTooLarge,
                    "Request body exceeds configured size limit.");
            }

            return await ProcessLeadAsync(dto, cancellationToken);
        }

        /// <summary>
        /// Combined IndiaMART Integration Health Check.
        /// Reports operational status of both Pull and Push mechanisms.
        /// </summary>
        [HttpGet("health")]
        [Produces("application/json")]
        public async Task<IActionResult> Health(CancellationToken cancellationToken)
        {
            var pullSource = await _db.LeadSyncSources.AsNoTracking()
                .Include(s => s.Config)
                .Include(s => s.Credentials)
                .FirstOrDefaultAsync(s => s.Code == "indiamart", cancellationToken);

            var metricsSnapshot = _metrics.GetSnapshot();
            var webhookUrl = BuildPublicWebhookUrl();

            var healthReport = new
            {
                provider = "IndiaMART",
                timestampUtc = DateTimeOffset.UtcNow,
                pull = new
                {
                    enabled = pullSource?.Config?.AutoSyncEnabled ?? false,
                    configured = pullSource?.ApiIntegrationReady ?? false,
                    lastSyncAt = pullSource?.Config?.LastSyncAt,
                    nextSyncAt = pullSource?.Config?.NextSyncAt
                },
                push = new
                {
                    enabled = _options.Enabled,
                    requireApiKey = _options.RequireApiKey,
                    requireIpWhitelist = _options.RequireIpWhitelist,
                    webhookUrl,
                    lastReceivedAtUtc = metricsSnapshot.LastReceivedAtUtc,
                    lastSuccessAtUtc = metricsSnapshot.LastSuccessAtUtc,
                    lastFailureAtUtc = metricsSnapshot.LastFailureAtUtc,
                    totalReceived = metricsSnapshot.TotalReceived,
                    inserted = metricsSnapshot.Inserted,
                    duplicates = metricsSnapshot.Duplicates
                }
            };

            return Ok(healthReport);
        }

        /// <summary>
        /// Protected metrics counters for observability.
        /// Requires valid webhook authorization key or CRM admin credentials.
        /// </summary>
        [HttpGet("metrics")]
        [Produces("application/json")]
        public IActionResult Metrics()
        {
            var correlationId = ResolveCorrelationId();
            Response.Headers[_options.CorrelationIdHeaderName] = correlationId;

            var security = _security.Evaluate(Request);
            if (security.Status != IndiaMartWebhookSecurityStatus.Allowed)
            {
                return Reject(security.Status, security.Message, correlationId);
            }

            return Ok(_metrics.GetSnapshot());
        }

        /// <summary>
        /// Admin test facility. Allows sending a test IndiaMART payload.
        /// Gated by webhook security. Prefixes test identifier (TEST-IM-...)
        /// and supports ?dryRun=true to validate payload mapping without saving to database.
        /// </summary>
        [HttpPost("test")]
        [Produces("application/json")]
        public async Task<IActionResult> TestPush(
            [FromBody] IndiaMartWebhookLeadDto? dto,
            [FromQuery] bool dryRun = false,
            CancellationToken cancellationToken = default)
        {
            var correlationId = ResolveCorrelationId();
            Response.Headers[_options.CorrelationIdHeaderName] = correlationId;

            var security = _security.Evaluate(Request);
            if (security.Status != IndiaMartWebhookSecurityStatus.Allowed)
            {
                return Reject(security.Status, security.Message, correlationId);
            }

            dto ??= new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = $"TEST-IM-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                SenderName = "Test Buyer",
                SenderMobile = "9999999999",
                SenderEmail = "test.buyer@example.com",
                Subject = "Simulated IndiaMART Inquiry",
                QueryProductName = "Test Product",
                QueryMessage = "This is a simulated webhook delivery for testing.",
                GlusrUsrCompanyName = "Test Enterprise Ltd",
                SenderCity = "Mumbai"
            };

            if (string.IsNullOrWhiteSpace(dto.UniqueQueryId))
            {
                dto.UniqueQueryId = $"TEST-IM-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            }
            else if (!dto.UniqueQueryId.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase))
            {
                dto.UniqueQueryId = $"TEST-{dto.UniqueQueryId}";
            }

            if (dryRun)
            {
                var mapped = MarketplaceLeadMapper.FromIndiaMartPush(dto);
                return Ok(new
                {
                    test = true,
                    dryRun = true,
                    correlationId,
                    status = "VALIDATED_DRY_RUN",
                    mappedLead = new
                    {
                        mapped.ExternalKey,
                        mapped.FirstName,
                        mapped.LastName,
                        mapped.Email,
                        mapped.Mobile,
                        mapped.OrganizationName,
                        mapped.Requirement,
                        mapped.Notes
                    },
                    message = "Payload parsed and validated successfully in dry-run mode (no CRM records created)."
                });
            }

            var requestContext = new IndiaMartWebhookRequestContext
            {
                CorrelationId = correlationId,
                RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                HttpMethod = Request.Method,
                TimestampUtc = DateTimeOffset.UtcNow
            };

            var result = await _service.ProcessAsync(dto, requestContext, cancellationToken);
            return Ok(new
            {
                test = true,
                dryRun = false,
                correlationId,
                result.Outcome,
                result.ExternalKey,
                result.LeadId,
                result.LeadOwnerId,
                result.IsDuplicate,
                result.RoundRobinAssigned,
                result.ContactCreated,
                result.Message
            });
        }

        private async Task<IActionResult> ProcessLeadAsync(
            IndiaMartWebhookLeadDto? dto,
            CancellationToken cancellationToken)
        {
            var correlationId = ResolveCorrelationId();
            Response.Headers[_options.CorrelationIdHeaderName] = correlationId;

            var security = _security.Evaluate(Request);
            if (security.Status != IndiaMartWebhookSecurityStatus.Allowed)
            {
                return Reject(security.Status, security.Message, correlationId);
            }

            var requestContext = new IndiaMartWebhookRequestContext
            {
                CorrelationId = correlationId,
                RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                HttpMethod = Request.Method,
                TimestampUtc = DateTimeOffset.UtcNow
            };

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_options.ProcessingTimeoutSeconds > 0)
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.ProcessingTimeoutSeconds));
            }

            var result = await _service.ProcessAsync(dto, requestContext, timeoutCts.Token);

            if (result.Outcome == IndiaMartWebhookProcessingOutcome.ValidationFailed)
            {
                return BadRequest(new
                {
                    status = "VALIDATION_FAILED",
                    message = result.Message,
                    correlationId
                });
            }

            if (result.Outcome == IndiaMartWebhookProcessingOutcome.PersistenceFailed)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = "PERSISTENCE_FAILED",
                    message = "Failed to process lead notification.",
                    correlationId
                });
            }

            return Ok(new
            {
                status = result.IsDuplicate ? "DUPLICATE_ACCEPTED" : "SUCCESS",
                message = result.Message,
                externalKey = result.ExternalKey,
                leadId = result.LeadId,
                leadOwnerId = result.LeadOwnerId,
                isDuplicate = result.IsDuplicate,
                roundRobinAssigned = result.RoundRobinAssigned,
                contactCreated = result.ContactCreated,
                correlationId
            });
        }

        private IActionResult Reject(
            IndiaMartWebhookSecurityStatus status,
            string message,
            string? correlationId = null)
        {
            correlationId ??= ResolveCorrelationId();
            Response.Headers[_options.CorrelationIdHeaderName] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Integration"] = "IndiaMartWebhook"
            }))
            {
                switch (status)
                {
                    case IndiaMartWebhookSecurityStatus.Disabled:
                        _metrics.IncrementSkippedDisabled();
                        _logger.LogWarning(
                            "Webhook disabled. CorrelationId={CorrelationId} Message={Message}",
                            correlationId,
                            message);
                        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                        {
                            status = "DISABLED",
                            message = "IndiaMART webhook integration is currently disabled.",
                            correlationId
                        });

                    case IndiaMartWebhookSecurityStatus.InvalidApiKey:
                        _metrics.IncrementAuthFailed();
                        _logger.LogWarning(
                            "Authentication failed. CorrelationId={CorrelationId} RemoteIp={RemoteIp}",
                            correlationId,
                            HttpContext.Connection.RemoteIpAddress?.ToString());
                        return Unauthorized(new
                        {
                            status = "UNAUTHORIZED",
                            message = "Invalid or missing webhook authentication key.",
                            correlationId
                        });

                    case IndiaMartWebhookSecurityStatus.IpNotAllowed:
                        _metrics.IncrementAuthFailed();
                        _logger.LogWarning(
                            "IP blocked. CorrelationId={CorrelationId} RemoteIp={RemoteIp}",
                            correlationId,
                            HttpContext.Connection.RemoteIpAddress?.ToString());
                        return StatusCode(StatusCodes.Status403Forbidden, new
                        {
                            status = "FORBIDDEN",
                            message = "Client IP address is not permitted.",
                            correlationId
                        });

                    case IndiaMartWebhookSecurityStatus.PayloadTooLarge:
                        _metrics.IncrementMalformed();
                        _logger.LogWarning(
                            "Payload too large. CorrelationId={CorrelationId}",
                            correlationId);
                        return StatusCode(StatusCodes.Status413PayloadTooLarge, new
                        {
                            status = "PAYLOAD_TOO_LARGE",
                            message = "Request body size exceeds maximum limit.",
                            correlationId
                        });

                    case IndiaMartWebhookSecurityStatus.Malformed:
                        _metrics.IncrementMalformed();
                        _logger.LogWarning(
                            "Malformed request. CorrelationId={CorrelationId} Message={Message}",
                            correlationId,
                            message);
                        return BadRequest(new
                        {
                            status = "MALFORMED",
                            message,
                            correlationId
                        });

                    default:
                        _metrics.IncrementAuthFailed();
                        return Unauthorized(new
                        {
                            status = "UNAUTHORIZED",
                            message = "Authentication failure.",
                            correlationId
                        });
                }
            }
        }

        private string BuildPublicWebhookUrl()
        {
            var path = "/api/integrations/indiamart/leads";
            if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            {
                return $"{_options.PublicBaseUrl.TrimEnd('/')}{path}";
            }

            var request = HttpContext?.Request;
            if (request != null)
            {
                return $"{request.Scheme}://{request.Host}{path}";
            }

            return $"https://<your-public-domain>{path}";
        }

        private string ResolveCorrelationId()
        {
            var headerName = string.IsNullOrWhiteSpace(_options.CorrelationIdHeaderName)
                ? "X-Correlation-Id"
                : _options.CorrelationIdHeaderName;

            if (Request.Headers.TryGetValue(headerName, out var values))
            {
                var existing = values.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(existing))
                {
                    return existing.Trim();
                }
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
