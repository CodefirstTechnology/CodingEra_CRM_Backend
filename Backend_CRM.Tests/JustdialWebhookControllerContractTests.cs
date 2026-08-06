using System.Text.Json;
using CRM.Configuration;
using CRM.Controllers;
using CRM.DTO;
using CRM.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backend_CRM.Tests;

/// <summary>
/// Controller/service contract tests. GET, POST JSON, POST form response contract remains RECEIVED.
/// </summary>
public class JustdialWebhookControllerContractTests
{
    [Fact]
    public void PostJson_returns_received_for_numeric_compatible_dnc_values()
    {
        var metrics = new JustdialWebhookMetrics();
        var service = new CapturingWebhookService();
        var controller = CreateController(service, metrics);

        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JDF99CB59600766",
            Name = "Messaging6",
            Mobile = "9820778865",
            Dncmobile = "0",
            Dncphone = "0",
            State = "Maharashtra"
        };

        var result = controller.PostJson(dto, CancellationToken.None).GetAwaiter().GetResult();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("RECEIVED", content.Content);
        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public void Get_returns_received()
    {
        var metrics = new JustdialWebhookMetrics();
        var service = new CapturingWebhookService();
        var controller = CreateController(service, metrics);

        var result = controller.Get(
            new JustdialWebhookLeadDto
            {
                Leadid = "JD-GET-1",
                Name = "Ada Lovelace",
                Mobile = "9999999999"
            },
            CancellationToken.None).GetAwaiter().GetResult();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("RECEIVED", content.Content);
        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public void PostForm_returns_received()
    {
        var metrics = new JustdialWebhookMetrics();
        var service = new CapturingWebhookService();
        var controller = CreateController(service, metrics);

        var result = controller.PostForm(
            new JustdialWebhookLeadDto
            {
                Leadid = "JD-FORM-1",
                Name = "Form User",
                Mobile = "8888888888",
                Dncmobile = "0"
            },
            CancellationToken.None).GetAwaiter().GetResult();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("RECEIVED", content.Content);
        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public void Missing_required_fields_returns_received_and_increments_validationFailed()
    {
        var metrics = new JustdialWebhookMetrics();
        var realService = new JustdialWebhookService(
            new NoOpMarketplacePersistence(),
            metrics,
            Options.Create(new JustdialWebhookOptions { Enabled = true, RequireApiKey = false }),
            NullLogger<JustdialWebhookService>.Instance);

        var controller = CreateController(realService, metrics);

        var result = controller.PostJson(
            new JustdialWebhookLeadDto(),
            CancellationToken.None).GetAwaiter().GetResult();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("RECEIVED", content.Content);
        Assert.Equal(1L, metrics.GetSnapshot().TotalReceived);
        Assert.Equal(1L, metrics.GetSnapshot().ValidationFailed);
        Assert.Equal(0L, metrics.GetSnapshot().Inserted);
    }

    [Fact]
    public void Empty_email_with_required_fields_still_returns_received()
    {
        var metrics = new JustdialWebhookMetrics();
        var service = new CapturingWebhookService();
        var controller = CreateController(service, metrics);

        var result = controller.PostJson(
            new JustdialWebhookLeadDto
            {
                Leadid = "JD-EMPTY-EMAIL",
                Name = "No Email",
                Mobile = "7777777777",
                Email = ""
            },
            CancellationToken.None).GetAwaiter().GetResult();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("RECEIVED", content.Content);
    }

    [Fact]
    public void Unicode_and_special_characters_return_received()
    {
        var metrics = new JustdialWebhookMetrics();
        var service = new CapturingWebhookService();
        var controller = CreateController(service, metrics);

        var result = controller.PostJson(
            new JustdialWebhookLeadDto
            {
                Leadid = "JD-UNI-<>&'\"",
                Name = "राहुल Sharma <script>alert(1)</script>",
                Mobile = "6666666666",
                Category = "Generators & Motors",
                Area = "Ghatkopar West / मुंबई"
            },
            CancellationToken.None).GetAwaiter().GetResult();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("RECEIVED", content.Content);
    }

    [Fact]
    public void Sql_injection_and_xss_payloads_still_return_received()
    {
        var metrics = new JustdialWebhookMetrics();
        var service = new CapturingWebhookService();
        var controller = CreateController(service, metrics);

        var result = controller.PostJson(
            new JustdialWebhookLeadDto
            {
                Leadid = "JD'; DROP TABLE leads;--",
                Name = "<img src=x onerror=alert(1)>",
                Mobile = "5555555555",
                Company = "'; SELECT * FROM users--"
            },
            CancellationToken.None).GetAwaiter().GetResult();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("RECEIVED", content.Content);
    }

    [Fact]
    public void Metrics_endpoint_returns_snapshot_when_security_allows()
    {
        var metrics = new JustdialWebhookMetrics();
        metrics.IncrementReceived();
        var controller = CreateController(new CapturingWebhookService(), metrics);

        var result = controller.Metrics();

        var ok = Assert.IsType<OkObjectResult>(result);
        var snapshot = Assert.IsType<JustdialWebhookMetricsSnapshot>(ok.Value);
        Assert.Equal(1L, snapshot.TotalReceived);
    }

    private static JustdialWebhookController CreateController(
        IJustdialWebhookService service,
        IJustdialWebhookMetrics metrics)
    {
        var options = Options.Create(new JustdialWebhookOptions
        {
            Enabled = true,
            RequireApiKey = false,
            RequireIpWhitelist = false,
            MaxRequestBodyBytes = 65536,
            ProcessingTimeoutSeconds = 30,
            CorrelationIdHeaderName = "X-Correlation-Id"
        });

        return new JustdialWebhookController(
            service,
            new JustdialWebhookSecurityService(options),
            metrics,
            options,
            NullLogger<JustdialWebhookController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class CapturingWebhookService : IJustdialWebhookService
    {
        public int CallCount { get; private set; }

        public Task ProcessAsync(
            JustdialWebhookLeadDto? dto,
            JustdialWebhookRequestContext requestContext,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpMarketplacePersistence : IMarketplaceLeadPersistenceService
    {
        public Task<MarketplaceLeadPersistBatchResult> PersistAsync(
            string markerName,
            string leadSource,
            IReadOnlyList<LeadSyncIncomingLead> incoming,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new MarketplaceLeadPersistBatchResult());

        public Task<MarketplaceLeadPersistItemResult> PersistOneAsync(
            string markerName,
            string leadSource,
            LeadSyncIncomingLead incoming,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new MarketplaceLeadPersistItemResult
            {
                Outcome = MarketplaceLeadPersistOutcome.Created,
                ExternalKey = incoming.ExternalKey,
                LeadId = 1
            });
    }
}

/// <summary>
/// Confirms ASP.NET Core System.Text.Json options used by controllers deserialize Justdial dual-typed fields.
/// </summary>
public class JustdialWebhookAspNetJsonOptionsTests
{
    [Fact]
    public void Controller_json_options_deserialize_production_numeric_dnc_payload()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        const string json = """
            {
              "leadid": "JDF99CB59600766",
              "name": "Messaging6",
              "mobile": "9820778865",
              "dncmobile": 0,
              "dncphone": 0,
              "pincode": "0",
              "branchpin": "400001",
              "state": "Maharashtra"
            }
            """;

        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, options);

        Assert.NotNull(dto);
        Assert.Equal("0", dto!.Dncmobile);
        Assert.Equal("0", dto.Dncphone);
        Assert.Equal("Maharashtra", dto.State);
    }

    [Fact]
    public void Controller_json_options_deserialize_string_and_numeric_pins()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        const string json = """
            {
              "leadid": "JD-MIX",
              "name": "Mix",
              "mobile": "1111111111",
              "dncmobile": "1",
              "dncphone": 1,
              "pincode": 411038,
              "branchpin": "411001"
            }
            """;

        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, options);

        Assert.NotNull(dto);
        Assert.Equal("1", dto!.Dncmobile);
        Assert.Equal("1", dto.Dncphone);
        Assert.Equal("411038", dto.Pincode);
        Assert.Equal("411001", dto.Branchpin);
    }
}
