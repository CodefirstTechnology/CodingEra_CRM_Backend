using System.Text.Json;
using CRM.DTO;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    /// <summary>
    /// Inbound Justdial webhook endpoint. Always returns plain text RECEIVED.
    /// Lead persistence is handled by <see cref="IJustdialWebhookService"/>.
    /// </summary>
    [Route("api/integrations/justdial/leads")]
    [ApiController]
    public class JustdialWebhookController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IJustdialWebhookService _service;
        private readonly ILogger<JustdialWebhookController> _logger;

        public JustdialWebhookController(
            IJustdialWebhookService service,
            ILogger<JustdialWebhookController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Justdial GET push (query string parameters).
        /// </summary>
        [HttpGet]
        public Task<ContentResult> Get([FromQuery] JustdialWebhookLeadDto dto, CancellationToken cancellationToken)
        {
            return ProcessAndRespondAsync(dto, cancellationToken);
        }

        /// <summary>
        /// Justdial POST push with JSON body.
        /// </summary>
        [HttpPost]
        [Consumes("application/json")]
        public async Task<ContentResult> PostJson(CancellationToken cancellationToken)
        {
            JustdialWebhookLeadDto? dto = null;

            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(body, JsonOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while deserializing Justdial JSON webhook payload.");
                return PlainReceived();
            }

            return await ProcessAndRespondAsync(dto, cancellationToken);
        }

        /// <summary>
        /// Justdial POST push with form-urlencoded body.
        /// </summary>
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ContentResult> PostForm(CancellationToken cancellationToken)
        {
            JustdialWebhookLeadDto? dto = null;

            try
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                dto = MapFromForm(form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while reading Justdial form-urlencoded webhook payload.");
                return PlainReceived();
            }

            return await ProcessAndRespondAsync(dto, cancellationToken);
        }

        private async Task<ContentResult> ProcessAndRespondAsync(
            JustdialWebhookLeadDto? dto,
            CancellationToken cancellationToken)
        {
            try
            {
                await _service.ProcessAsync(dto, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while handling Justdial webhook request.");
            }

            return PlainReceived();
        }

        private static ContentResult PlainReceived() =>
            new()
            {
                Content = "RECEIVED",
                ContentType = "text/plain",
                StatusCode = StatusCodes.Status200OK
            };

        private static JustdialWebhookLeadDto MapFromForm(IFormCollection form) =>
            new()
            {
                Leadid = GetFormValue(form, "leadid"),
                Leadtype = GetFormValue(form, "leadtype"),
                Prefix = GetFormValue(form, "prefix"),
                Name = GetFormValue(form, "name"),
                Mobile = GetFormValue(form, "mobile"),
                Phone = GetFormValue(form, "phone"),
                Email = GetFormValue(form, "email"),
                Date = GetFormValue(form, "date"),
                Category = GetFormValue(form, "category"),
                City = GetFormValue(form, "city"),
                Area = GetFormValue(form, "area"),
                Brancharea = GetFormValue(form, "brancharea"),
                Dncmobile = GetFormValue(form, "dncmobile"),
                Dncphone = GetFormValue(form, "dncphone"),
                Company = GetFormValue(form, "company"),
                Pincode = GetFormValue(form, "pincode"),
                Time = GetFormValue(form, "time"),
                Branchpin = GetFormValue(form, "branchpin"),
                Parentid = GetFormValue(form, "parentid")
            };

        private static string? GetFormValue(IFormCollection form, string key)
        {
            if (!form.TryGetValue(key, out var value))
            {
                return null;
            }

            var text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}
