using CRM.Configuration;
using CRM.DTO;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CRM.Controllers
{
    /// <summary>
    /// Inbound Justdial webhook. Success path returns plain text RECEIVED.
    /// Auth / enablement / oversized / malformed requests return non-RECEIVED status codes.
    /// </summary>
    [Route("api/integrations/justdial")]
    [ApiController]
    public class JustdialWebhookController : ControllerBase
    {
        private readonly IJustdialWebhookService _service;
        private readonly IJustdialWebhookSecurityService _security;
        private readonly IJustdialWebhookMetrics _metrics;
        private readonly JustdialWebhookOptions _options;
        private readonly ILogger<JustdialWebhookController> _logger;

        public JustdialWebhookController(
            IJustdialWebhookService service,
            IJustdialWebhookSecurityService security,
            IJustdialWebhookMetrics metrics,
            IOptions<JustdialWebhookOptions> options,
            ILogger<JustdialWebhookController> logger)
        {
            _service = service;
            _security = security;
            _metrics = metrics;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>Justdial GET push (query string parameters).</summary>
        [HttpGet("leads")]
        [Produces("text/plain")]
        public Task<IActionResult> Get([FromQuery] JustdialWebhookLeadDto dto, CancellationToken cancellationToken)
        {
            return ProcessAsync(dto, isMalformed: false, cancellationToken);
        }

        /// <summary>
        /// Justdial POST push (JSON). Use this action in Swagger Try it out.
        /// Required fields: leadid, name, mobile.
        /// </summary>
        /// <remarks>
        /// Sample request body:
        ///
        ///     {
        ///       "leadid": "PRELIVE-001",
        ///       "leadtype": "enquiry",
        ///       "prefix": "Mr",
        ///       "name": "Test User",
        ///       "mobile": "9876543210",
        ///       "phone": "02212345678",
        ///       "email": "test.justdial@example.com",
        ///       "date": "2026-07-16",
        ///       "category": "Software",
        ///       "city": "Pune",
        ///       "area": "Kothrud",
        ///       "brancharea": "West",
        ///       "dncmobile": "0",
        ///       "dncphone": "0",
        ///       "company": "Codefirst Test Co",
        ///       "pincode": "411038",
        ///       "time": "17:00",
        ///       "branchpin": "411001",
        ///       "parentid": "0"
        ///     }
        ///
        /// </remarks>
        [HttpPost("leads")]
        [Consumes("application/json")]
        [Produces("text/plain")]
        public Task<IActionResult> PostJson(
            [FromBody] JustdialWebhookLeadDto? dto,
            CancellationToken cancellationToken)
        {
            if (_options.MaxRequestBodyBytes > 0
                && Request.ContentLength is long length
                && length > _options.MaxRequestBodyBytes)
            {
                return Task.FromResult(
                    Reject(JustdialWebhookSecurityStatus.PayloadTooLarge, "Request body exceeds configured size limit."));
            }

            return ProcessAsync(dto, isMalformed: false, cancellationToken);
        }

        /// <summary>
        /// Justdial POST push (form-urlencoded). Hidden from Swagger to avoid duplicate path docs;
        /// runtime support remains for Justdial.
        /// </summary>
        [HttpPost("leads")]
        [Consumes("application/x-www-form-urlencoded")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Produces("text/plain")]
        public Task<IActionResult> PostForm(
            [FromForm] JustdialWebhookLeadDto dto,
            CancellationToken cancellationToken)
        {
            if (_options.MaxRequestBodyBytes > 0
                && Request.ContentLength is long length
                && length > _options.MaxRequestBodyBytes)
            {
                return Task.FromResult(
                    Reject(JustdialWebhookSecurityStatus.PayloadTooLarge, "Request body exceeds configured size limit."));
            }

            return ProcessAsync(dto, isMalformed: false, cancellationToken);
        }

        /// <summary>
        /// Process-local webhook counters (protected by the same API key / IP rules when enabled).
        /// </summary>
        [HttpGet("metrics")]
        public IActionResult Metrics()
        {
            var security = _security.Evaluate(Request);
            if (security.Status != JustdialWebhookSecurityStatus.Allowed)
            {
                return Reject(security.Status, security.Message);
            }

            return Ok(_metrics.GetSnapshot());
        }

        private async Task<IActionResult> ProcessAsync(
            JustdialWebhookLeadDto? dto,
            bool isMalformed,
            CancellationToken cancellationToken)
        {
            var correlationId = ResolveCorrelationId();
            Response.Headers[_options.CorrelationIdHeaderName] = correlationId;

            var security = _security.Evaluate(Request);
            if (security.Status != JustdialWebhookSecurityStatus.Allowed)
            {
                return Reject(security.Status, security.Message, correlationId);
            }

            if (isMalformed)
            {
                return Reject(JustdialWebhookSecurityStatus.Malformed, "Malformed request.", correlationId);
            }

            var requestContext = new JustdialWebhookRequestContext
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

            try
            {
                await _service.ProcessAsync(dto, requestContext, timeoutCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception while handling Justdial webhook request. CorrelationId={CorrelationId}",
                    correlationId);
            }

            return PlainReceived();
        }

        private IActionResult Reject(
            JustdialWebhookSecurityStatus status,
            string message,
            string? correlationId = null)
        {
            correlationId ??= ResolveCorrelationId();
            Response.Headers[_options.CorrelationIdHeaderName] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Integration"] = "JustdialWebhook"
            }))
            {
                switch (status)
                {
                    case JustdialWebhookSecurityStatus.Disabled:
                        _metrics.IncrementSkippedDisabled();
                        _logger.LogWarning(
                            "Webhook disabled. CorrelationId={CorrelationId} Message={Message}",
                            correlationId,
                            message);
                        return PlainStatus(StatusCodes.Status503ServiceUnavailable, "DISABLED");

                    case JustdialWebhookSecurityStatus.InvalidApiKey:
                        _metrics.IncrementAuthFailed();
                        _logger.LogWarning(
                            "Authentication failed. CorrelationId={CorrelationId} RemoteIp={RemoteIp}",
                            correlationId,
                            HttpContext.Connection.RemoteIpAddress?.ToString());
                        return PlainStatus(StatusCodes.Status401Unauthorized, "UNAUTHORIZED");

                    case JustdialWebhookSecurityStatus.IpNotAllowed:
                        _metrics.IncrementAuthFailed();
                        _logger.LogWarning(
                            "IP blocked. CorrelationId={CorrelationId} RemoteIp={RemoteIp}",
                            correlationId,
                            HttpContext.Connection.RemoteIpAddress?.ToString());
                        return PlainStatus(StatusCodes.Status403Forbidden, "FORBIDDEN");

                    case JustdialWebhookSecurityStatus.PayloadTooLarge:
                        _metrics.IncrementMalformed();
                        _logger.LogWarning(
                            "Payload too large. CorrelationId={CorrelationId}",
                            correlationId);
                        return PlainStatus(StatusCodes.Status413PayloadTooLarge, "PAYLOAD_TOO_LARGE");

                    case JustdialWebhookSecurityStatus.Malformed:
                        _metrics.IncrementMalformed();
                        _logger.LogWarning(
                            "Malformed request. CorrelationId={CorrelationId} Message={Message}",
                            correlationId,
                            message);
                        return PlainStatus(StatusCodes.Status400BadRequest, "MALFORMED");

                    default:
                        _metrics.IncrementAuthFailed();
                        return PlainStatus(StatusCodes.Status401Unauthorized, "UNAUTHORIZED");
                }
            }
        }

        private static ContentResult PlainStatus(int statusCode, string content) =>
            new()
            {
                Content = content,
                ContentType = "text/plain",
                StatusCode = statusCode
            };

        private static ContentResult PlainReceived() =>
            PlainStatus(StatusCodes.Status200OK, "RECEIVED");

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
