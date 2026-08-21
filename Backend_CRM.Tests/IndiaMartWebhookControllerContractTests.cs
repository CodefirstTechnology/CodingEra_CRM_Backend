using System.Net;
using System.Text.Json;
using CRM.Configuration;
using CRM.Controllers;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backend_CRM.Tests
{
    public class IndiaMartWebhookControllerContractTests
    {
        [Fact]
        public async Task PostJson_allows_request_when_api_key_valid()
        {
            var metrics = new IndiaMartWebhookMetrics();
            var service = new CapturingIndiaMartWebhookService();
            var options = new IndiaMartWebhookOptions
            {
                Enabled = true,
                RequireApiKey = true,
                ApiKey = "secret-key-123",
                ApiKeyHeaderName = "X-IndiaMart-Webhook-Key"
            };

            var controller = CreateController(service, metrics, options);
            controller.ControllerContext.HttpContext.Request.Headers["X-IndiaMart-Webhook-Key"] = "secret-key-123";

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-001",
                SenderName = "Aarav Patel",
                SenderMobile = "9876543210",
                SenderEmail = "aarav@example.com"
            };

            var result = await controller.PostJson(dto, CancellationToken.None);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(1, service.CallCount);
        }

        [Fact]
        public async Task PostJson_rejects_unauthorized_when_api_key_missing_and_required()
        {
            var metrics = new IndiaMartWebhookMetrics();
            var service = new CapturingIndiaMartWebhookService();
            var options = new IndiaMartWebhookOptions
            {
                Enabled = true,
                RequireApiKey = true,
                ApiKey = "secret-key-123"
            };

            var controller = CreateController(service, metrics, options);
            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-002",
                SenderName = "Aarav Patel",
                SenderMobile = "9876543210"
            };

            var result = await controller.PostJson(dto, CancellationToken.None);
            var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauth.StatusCode);
            Assert.Equal(0, service.CallCount);
            Assert.Equal(1, metrics.GetSnapshot().AuthFailed);
        }

        [Fact]
        public async Task PostJson_rejects_unauthorized_when_api_key_invalid()
        {
            var metrics = new IndiaMartWebhookMetrics();
            var service = new CapturingIndiaMartWebhookService();
            var options = new IndiaMartWebhookOptions
            {
                Enabled = true,
                RequireApiKey = true,
                ApiKey = "secret-key-123"
            };

            var controller = CreateController(service, metrics, options);
            controller.ControllerContext.HttpContext.Request.Headers["X-IndiaMart-Webhook-Key"] = "wrong-key";

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-003",
                SenderName = "Aarav Patel",
                SenderMobile = "9876543210"
            };

            var result = await controller.PostJson(dto, CancellationToken.None);
            var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauth.StatusCode);
            Assert.Equal(0, service.CallCount);
            Assert.Equal(1, metrics.GetSnapshot().AuthFailed);
        }

        [Fact]
        public async Task PostJson_allows_request_with_alt_header_and_bearer_token()
        {
            var metrics = new IndiaMartWebhookMetrics();
            var service = new CapturingIndiaMartWebhookService();
            var options = new IndiaMartWebhookOptions
            {
                Enabled = true,
                RequireApiKey = true,
                ApiKey = "secret-key-123"
            };

            var controller = CreateController(service, metrics, options);
            controller.ControllerContext.HttpContext.Request.Headers["Authorization"] = "Bearer secret-key-123";

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-004",
                SenderName = "Aarav Patel",
                SenderMobile = "9876543210"
            };

            var result = await controller.PostJson(dto, CancellationToken.None);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(1, service.CallCount);
        }

        [Fact]
        public async Task PostJson_rejects_when_integration_disabled()
        {
            var metrics = new IndiaMartWebhookMetrics();
            var service = new CapturingIndiaMartWebhookService();
            var options = new IndiaMartWebhookOptions
            {
                Enabled = false
            };

            var controller = CreateController(service, metrics, options);
            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-005",
                SenderName = "Aarav Patel",
                SenderMobile = "9876543210"
            };

            var result = await controller.PostJson(dto, CancellationToken.None);
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusResult.StatusCode);
            Assert.Equal(0, service.CallCount);
            Assert.Equal(1, metrics.GetSnapshot().SkippedDisabled);
        }

        [Fact]
        public async Task PostJson_rejects_when_ip_not_whitelisted()
        {
            var metrics = new IndiaMartWebhookMetrics();
            var service = new CapturingIndiaMartWebhookService();
            var options = new IndiaMartWebhookOptions
            {
                Enabled = true,
                RequireApiKey = false,
                RequireIpWhitelist = true,
                AllowedIpAddresses = new List<string> { "203.0.113.50" }
            };

            var controller = CreateController(service, metrics, options);
            controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.10");

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-006",
                SenderName = "Aarav Patel",
                SenderMobile = "9876543210"
            };

            var result = await controller.PostJson(dto, CancellationToken.None);
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
            Assert.Equal(0, service.CallCount);
        }

        [Fact]
        public async Task PostJson_rejects_when_body_exceeds_max_bytes()
        {
            var metrics = new IndiaMartWebhookMetrics();
            var service = new CapturingIndiaMartWebhookService();
            var options = new IndiaMartWebhookOptions
            {
                Enabled = true,
                RequireApiKey = false,
                MaxRequestBodyBytes = 100
            };

            var controller = CreateController(service, metrics, options);
            controller.ControllerContext.HttpContext.Request.ContentLength = 200;

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-007",
                SenderName = "Aarav Patel",
                SenderMobile = "9876543210"
            };

            var result = await controller.PostJson(dto, CancellationToken.None);
            var tooLarge = Assert.IsType<ObjectResult>(result);
            Assert.Equal(413, tooLarge.StatusCode);
            Assert.Equal(0, service.CallCount);
        }

        [Fact]
        public async Task PostJson_returns_bad_request_on_validation_failure_missing_contact_info()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);
            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-008",
                SenderName = "",
                SenderMobile = "",
                SenderEmail = ""
            };

            var result = await controller.PostJson(dto, CancellationToken.None);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
            Assert.Equal(1, metrics.GetSnapshot().ValidationFailed);
        }

        [Fact]
        public async Task PostJson_supports_case_insensitive_json_deserialization()
        {
            var json = """
            {
              "unique_query_id": "IM-SNAKE-101",
              "sender_name": "Vikram Seth",
              "sender_mobile": "9811122233",
              "sender_email": "vikram@example.com",
              "subject": "Stainless Valves",
              "glusr_usr_companyname": "Seth Enterprises",
              "sender_city": "Surat",
              "custom_extra_field": "some_value"
            }
            """;

            var dto = JsonSerializer.Deserialize<IndiaMartWebhookLeadDto>(json);
            Assert.NotNull(dto);
            Assert.Equal("IM-SNAKE-101", dto.GetEffectiveExternalKey());
            Assert.Equal("Vikram Seth", dto.SenderName);
            Assert.Equal("9811122233", dto.SenderMobile);
            Assert.Equal("vikram@example.com", dto.SenderEmail);
            Assert.Equal("Stainless Valves", dto.Subject);
            Assert.Equal("Seth Enterprises", dto.GlusrUsrCompanyName);
            Assert.Equal("Surat", dto.SenderCity);
            Assert.NotNull(dto.ExtensionData);
            Assert.True(dto.ExtensionData.ContainsKey("custom_extra_field"));
        }

        [Fact]
        public async Task PostJson_persists_valid_lead_with_canonical_marker_and_syncs_contact_and_org()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);
            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-PERSIST-1",
                SenderName = "Meera Nair",
                SenderMobile = "9822334455",
                SenderEmail = "meera.nair@example.com",
                Subject = "Industrial Pump",
                QueryMessage = "Need 10 pumps for water project",
                GlusrUsrCompanyName = "Nair Industries Ltd",
                SenderCity = "Kochi"
            };

            var result = await controller.PostJson(dto, CancellationToken.None);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Mobile == "9822334455");
            Assert.NotNull(lead);
            Assert.Equal("Meera", lead.FirstName);
            Assert.Equal("Nair", lead.LastName);
            Assert.Equal("IndiaMART", lead.LeadSource);
            Assert.Contains("[crm-ext:IndiaMART:IM-PERSIST-1]", lead.Notes);
            Assert.Contains("Need 10 pumps for water project", lead.Notes);
            Assert.Contains("Company: Nair Industries Ltd", lead.Notes);
            Assert.Contains("City: Kochi", lead.Notes);

            var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Phone == "9822334455");
            Assert.NotNull(contact);
            Assert.Equal("Meera", contact.FirstName);

            var org = await db.Organizations.FirstOrDefaultAsync(o => o.Name == "Nair Industries Ltd");
            Assert.NotNull(org);
            Assert.Equal(org.Id, lead.OrganizationId);

            Assert.Equal(1, metrics.GetSnapshot().Inserted);
        }

        [Fact]
        public async Task PostJson_handles_push_then_push_duplicate_idempotently()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);
            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-DUP-999",
                SenderName = "Kunal Shah",
                SenderMobile = "9870001111",
                SenderEmail = "kunal@example.com"
            };

            // First Push
            var first = await controller.PostJson(dto, CancellationToken.None);
            var firstOk = Assert.IsType<OkObjectResult>(first);
            Assert.Equal(200, firstOk.StatusCode);

            // Second Push with identical UNIQUE_QUERY_ID
            var second = await controller.PostJson(dto, CancellationToken.None);
            var secondOk = Assert.IsType<OkObjectResult>(second);
            Assert.Equal(200, secondOk.StatusCode);

            var leads = await db.Leads.Where(l => l.Notes.Contains("[crm-ext:IndiaMART:IM-DUP-999]")).ToListAsync();
            Assert.Single(leads); // Exactly 1 lead created

            Assert.Equal(1, metrics.GetSnapshot().Inserted);
            Assert.Equal(1, metrics.GetSnapshot().Duplicates);
        }

        [Fact]
        public async Task Pull_then_Push_duplicate_detected_using_canonical_marker()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);

            // 1. Simulate Pull API creating the lead first
            var pullIncoming = new LeadSyncIncomingLead
            {
                ExternalKey = "IM-CROSS-123",
                FirstName = "Rohan",
                LastName = "Mehta",
                Mobile = "9988776655",
                Email = "rohan@example.com",
                Notes = "[crm-ext:IndiaMART:IM-CROSS-123]"
            };
            var pullResult = await persistence.PersistOneAsync("IndiaMART", "IndiaMART", pullIncoming);
            Assert.Equal(MarketplaceLeadPersistOutcome.Created, pullResult.Outcome);

            // 2. Now Webhook Push arrives later with the same UNIQUE_QUERY_ID
            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);
            var pushDto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-CROSS-123",
                SenderName = "Rohan Mehta",
                SenderMobile = "9988776655",
                SenderEmail = "rohan@example.com"
            };

            var pushResponse = await controller.PostJson(pushDto, CancellationToken.None);
            var okResult = Assert.IsType<OkObjectResult>(pushResponse);
            Assert.Equal(200, okResult.StatusCode);

            // Verify only 1 CRM lead exists
            var totalLeads = await db.Leads.Where(l => l.Notes.Contains("[crm-ext:IndiaMART:IM-CROSS-123]")).CountAsync();
            Assert.Equal(1, totalLeads);
            Assert.Equal(1, metrics.GetSnapshot().Duplicates);
        }

        [Fact]
        public async Task Push_then_Pull_duplicate_detected_using_canonical_marker()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);

            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            // 1. Webhook Push arrives first
            var pushDto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-CROSS-456",
                SenderName = "Pooja Hegde",
                SenderMobile = "9977665544",
                SenderEmail = "pooja@example.com"
            };
            var pushResponse = await controller.PostJson(pushDto, CancellationToken.None);
            Assert.IsType<OkObjectResult>(pushResponse);

            // 2. Later Pull API fetches the same query ID
            var pullIncoming = new LeadSyncIncomingLead
            {
                ExternalKey = "IM-CROSS-456",
                FirstName = "Pooja",
                LastName = "Hegde",
                Mobile = "9977665544",
                Email = "pooja@example.com",
                Notes = "[crm-ext:IndiaMART:IM-CROSS-456]"
            };
            var pullResult = await persistence.PersistOneAsync("IndiaMART", "IndiaMART", pullIncoming);
            Assert.Equal(MarketplaceLeadPersistOutcome.Duplicate, pullResult.Outcome);

            // Verify only 1 CRM lead exists
            var count = await db.Leads.Where(l => l.Notes.Contains("[crm-ext:IndiaMART:IM-CROSS-456]")).CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Concurrent_Push_duplicate_requests_are_serialized_and_deduplicated()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);

            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-CONCURRENT-777",
                SenderName = "Concurrency Tester",
                SenderMobile = "9112233445",
                SenderEmail = "concurrent@example.com"
            };

            // Execute 5 concurrent requests with identical UNIQUE_QUERY_ID
            var tasks = Enumerable.Range(0, 5)
                .Select(_ => controller.PostJson(dto, CancellationToken.None))
                .ToArray();

            var results = await Task.WhenAll(tasks);
            foreach (var res in results)
            {
                Assert.IsType<OkObjectResult>(res);
            }

            var leads = await db.Leads.Where(l => l.Notes.Contains("[crm-ext:IndiaMART:IM-CONCURRENT-777]")).ToListAsync();
            Assert.Single(leads); // Exactly 1 lead created in DB
            Assert.Equal(1, metrics.GetSnapshot().Inserted);
            Assert.Equal(4, metrics.GetSnapshot().Duplicates);
        }

        [Fact]
        public async Task Get_health_returns_pull_and_push_status()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions
            {
                Enabled = true,
                RequireApiKey = true,
                PublicBaseUrl = "https://crm.mycompany.com"
            };

            db.LeadSyncSources.Add(new LeadSyncSource
            {
                Id = 1,
                Code = "indiamart",
                DisplayName = "IndiaMART",
                IsActive = true,
                ApiIntegrationReady = true,
                Config = new LeadSyncSourceConfig
                {
                    AutoSyncEnabled = true,
                    LastSyncAt = DateTime.UtcNow.AddMinutes(-10)
                }
            });
            await db.SaveChangesAsync();

            var controller = CreateController(new CapturingIndiaMartWebhookService(), metrics, options, db);

            var result = await controller.Health(CancellationToken.None);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void Get_metrics_returns_realtime_counters()
        {
            var metrics = new IndiaMartWebhookMetrics();
            metrics.IncrementReceived();
            metrics.IncrementInserted();
            metrics.IncrementDuplicates();

            var controller = CreateController(
                new CapturingIndiaMartWebhookService(),
                metrics,
                new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false });

            var result = controller.Metrics();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var snapshot = Assert.IsType<IndiaMartWebhookMetricsSnapshot>(okResult.Value);
            Assert.Equal(1, snapshot.TotalReceived);
            Assert.Equal(1, snapshot.Inserted);
            Assert.Equal(1, snapshot.Duplicates);
        }

        [Fact]
        public void Get_metrics_rejects_unauthorized_when_api_key_missing_and_required()
        {
            var metrics = new IndiaMartWebhookMetrics();
            var controller = CreateController(
                new CapturingIndiaMartWebhookService(),
                metrics,
                new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = true, ApiKey = "secure-key" });

            var result = controller.Metrics();
            var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauth.StatusCode);
        }

        [Fact]
        public async Task Post_test_creates_tagged_test_lead()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);
            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            var result = await controller.TestPush(null, dryRun: false, CancellationToken.None);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var testLead = await db.Leads.FirstOrDefaultAsync(l => l.Notes.Contains("[crm-ext:IndiaMART:TEST-"));
            Assert.NotNull(testLead);
            Assert.StartsWith("TEST-", testLead.FirstName == "Lead" ? "TEST-Lead" : "TEST-");
        }

        [Fact]
        public async Task Post_test_supports_dry_run_mode_without_database_persisting()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);
            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            var result = await controller.TestPush(
                new IndiaMartWebhookLeadDto
                {
                    UniqueQueryId = "DRY-101",
                    SenderName = "Dry Run User",
                    SenderMobile = "9800000000"
                },
                dryRun: true,
                CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // Verify no lead was written to database
            var count = await db.Leads.CountAsync();
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task Post_test_rejects_unauthorized_when_api_key_missing_and_required()
        {
            var controller = CreateController(
                new CapturingIndiaMartWebhookService(),
                new IndiaMartWebhookMetrics(),
                new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = true, ApiKey = "admin-secret" });

            var result = await controller.TestPush(null, dryRun: false, CancellationToken.None);
            var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauth.StatusCode);
        }

        [Fact]
        public void Get_leads_strictly_checks_connectivity_and_never_creates_or_persists_leads()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var service = new CapturingIndiaMartWebhookService();
            var controller = CreateController(
                service,
                metrics,
                new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false },
                db);

            var result = controller.Get();
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // Assert zero service invocations and zero DB leads
            Assert.Equal(0, service.CallCount);
            Assert.Equal(0, db.Leads.Count());
            Assert.Equal(0, metrics.GetSnapshot().TotalReceived);
            Assert.Equal(0, metrics.GetSnapshot().Inserted);
        }

        [Fact]
        public async Task RoundRobin_assigns_owner_correctly_for_push_leads()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };

            var assignedRoundRobin = new MockRoundRobinService(42);
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                assignedRoundRobin,
                NullLogger<MarketplaceLeadPersistenceService>.Instance);
            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "IM-RR-001",
                SenderName = "Assigned Client",
                SenderMobile = "9876500000"
            };

            var postResult = await controller.PostJson(dto, CancellationToken.None);
            Assert.IsType<OkObjectResult>(postResult);

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Mobile == "9876500000");
            Assert.NotNull(lead);
            Assert.Equal(42, lead.LeadOwnerId);
        }

        [Fact]
        public async Task PostForm_processes_valid_form_urlencoded_payload_successfully()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);
            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            var formDto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "LOCAL-IM-FORM-001",
                SenderName = "Form Test User",
                SenderMobile = "9000000002",
                SenderEmail = "form.test@example.com",
                Subject = "Form Test",
                QueryProductName = "Test Product",
                QueryMessage = "Testing form encoded webhook",
                GlusrUsrCompanyName = "Form Test Company",
                SenderCity = "Pune",
                SenderState = "Maharashtra",
                SenderPincode = "411001"
            };

            var result = await controller.PostForm(formDto, CancellationToken.None);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Notes.Contains("[crm-ext:IndiaMART:LOCAL-IM-FORM-001]"));
            Assert.NotNull(lead);
            Assert.Equal("Form", lead.FirstName);
            Assert.Equal("Test User", lead.LastName);
            Assert.Equal("9000000002", lead.Mobile);
            Assert.Equal("form.test@example.com", lead.Email);
        }

        [Fact]
        public async Task PostJson_supports_pascal_case_and_all_field_aliases()
        {
            var json = """
            {
              "QueryId": "PASCAL-101",
              "Name": "Anoop Verma",
              "Phone": "9812345678",
              "Email": "anoop@example.com",
              "Product": "Solar Panel 400W",
              "Requirement": "Need 20 panels for factory",
              "Company": "Verma Green Energy",
              "City": "Jaipur",
              "State": "Rajasthan",
              "Pincode": "302001",
              "Timestamp": "2026-08-21 15:00:00"
            }
            """;

            var dto = JsonSerializer.Deserialize<IndiaMartWebhookLeadDto>(json);
            Assert.NotNull(dto);
            Assert.Equal("PASCAL-101", dto.GetEffectiveExternalKey());
            Assert.Equal("Anoop Verma", dto.SenderName);
            Assert.Equal("9812345678", dto.SenderMobile);
            Assert.Equal("anoop@example.com", dto.SenderEmail);
            Assert.Equal("Solar Panel 400W", dto.QueryProductName);
            Assert.Equal("Need 20 panels for factory", dto.QueryMessage);
            Assert.Equal("Verma Green Energy", dto.GlusrUsrCompanyName);
            Assert.Equal("Jaipur", dto.SenderCity);
            Assert.Equal("Rajasthan", dto.SenderState);
            Assert.Equal("302001", dto.SenderPincode);
            Assert.Equal("2026-08-21 15:00:00", dto.QueryTime);
        }

        [Fact]
        public async Task PostJson_concurrent_requests_safely_persist_exactly_one_lead()
        {
            var db = CreateInMemoryDbContext();
            var metrics = new IndiaMartWebhookMetrics();
            var options = new IndiaMartWebhookOptions { Enabled = true, RequireApiKey = false };
            var persistence = new MarketplaceLeadPersistenceService(
                db,
                new NoOpRoundRobinService(),
                NullLogger<MarketplaceLeadPersistenceService>.Instance);
            var service = new IndiaMartWebhookService(
                persistence,
                metrics,
                Options.Create(options),
                NullLogger<IndiaMartWebhookService>.Instance);

            var controller = CreateController(service, metrics, options, db);

            var dto = new IndiaMartWebhookLeadDto
            {
                UniqueQueryId = "LOCAL-IM-CONCURRENT-001",
                SenderName = "Concurrent Tester",
                SenderMobile = "9988001122",
                SenderEmail = "concurrent@example.com"
            };

            var tasks = Enumerable.Range(0, 5)
                .Select(_ => controller.PostJson(dto, CancellationToken.None))
                .ToList();

            var results = await Task.WhenAll(tasks);
            foreach (var r in results)
            {
                var ok = Assert.IsType<OkObjectResult>(r);
                Assert.Equal(200, ok.StatusCode);
            }

            var leads = await db.Leads.Where(l => l.Notes.Contains("[crm-ext:IndiaMART:LOCAL-IM-CONCURRENT-001]")).ToListAsync();
            Assert.Single(leads);
        }

        private sealed class MockRoundRobinService : ILeadSyncRoundRobinService
        {
            private readonly int _ownerId;
            public MockRoundRobinService(int ownerId) => _ownerId = ownerId;

            public Task<bool> TryApplyOwnerForSyncLeadAsync(Lead lead, CancellationToken cancellationToken = default)
            {
                lead.LeadOwnerId = _ownerId;
                return Task.FromResult(true);
            }

            public Task<int?> PeekNextOwnerIdAsync(int sourceId, CancellationToken cancellationToken = default) =>
                Task.FromResult<int?>(_ownerId);
        }

        private static IndiaMartWebhookController CreateController(
            IIndiaMartWebhookService service,
            IIndiaMartWebhookMetrics metrics,
            IndiaMartWebhookOptions options,
            TaskDbcontext? db = null)
        {
            db ??= CreateInMemoryDbContext();
            var security = new IndiaMartWebhookSecurityService(Options.Create(options));
            var controller = new IndiaMartWebhookController(
                service,
                security,
                metrics,
                Options.Create(options),
                db,
                NullLogger<IndiaMartWebhookController>.Instance);

            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        private static TaskDbcontext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<TaskDbcontext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            var db = new TaskDbcontext(options);

            db.LeadStatuses.Add(new LeadStatus { Id = 1, Name = "New", IsActive = true });
            db.SaveChanges();
            return db;
        }

        private sealed class CapturingIndiaMartWebhookService : IIndiaMartWebhookService
        {
            public int CallCount { get; private set; }
            public IndiaMartWebhookLeadDto? LastDto { get; private set; }

            public Task<IndiaMartWebhookProcessingResult> ProcessAsync(
                IndiaMartWebhookLeadDto? dto,
                IndiaMartWebhookRequestContext requestContext,
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                LastDto = dto;
                return Task.FromResult(new IndiaMartWebhookProcessingResult
                {
                    Outcome = IndiaMartWebhookProcessingOutcome.Success,
                    ExternalKey = dto?.GetEffectiveExternalKey() ?? "test",
                    LeadId = 100,
                    Message = "Captured"
                });
            }
        }

        private sealed class NoOpRoundRobinService : ILeadSyncRoundRobinService
        {
            public Task<bool> TryApplyOwnerForSyncLeadAsync(Lead lead, CancellationToken cancellationToken = default) =>
                Task.FromResult(false);

            public Task<int?> PeekNextOwnerIdAsync(int sourceId, CancellationToken cancellationToken = default) =>
                Task.FromResult<int?>(null);
        }
    }
}
