using System.Text.Json;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend_CRM.Tests;

public class MarketplaceLeadMapperJustdialTests
{
    [Fact]
    public void FromJustdial_prioritizes_mobile_over_phone_and_stores_secondary_phone_in_raw_payload_when_distinct()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-TEST-1",
            Name = "John Doe",
            Mobile = "9820778865",
            Phone = "0221234567"
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        Assert.Equal("9820778865", lead.Mobile);
        Assert.NotNull(lead.RawPayload);

        using var doc = JsonDocument.Parse(lead.RawPayload!);
        Assert.True(doc.RootElement.TryGetProperty("phone", out var phoneProp));
        Assert.Equal("0221234567", phoneProp.GetString());
    }

    [Fact]
    public void FromJustdial_falls_back_to_phone_when_mobile_is_empty()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-TEST-2",
            Name = "Jane Doe",
            Mobile = "",
            Phone = "9820778865"
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        Assert.Equal("9820778865", lead.Mobile);
        if (lead.RawPayload != null)
        {
            using var doc = JsonDocument.Parse(lead.RawPayload);
            Assert.False(doc.RootElement.TryGetProperty("phone", out _));
        }
    }

    [Fact]
    public void FromJustdial_does_not_duplicate_phone_in_raw_payload_when_mobile_and_phone_are_identical()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-TEST-3",
            Name = "Same Phone",
            Mobile = "9820778865",
            Phone = "9820778865"
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        Assert.Equal("9820778865", lead.Mobile);
        if (lead.RawPayload != null)
        {
            using var doc = JsonDocument.Parse(lead.RawPayload);
            Assert.False(doc.RootElement.TryGetProperty("phone", out _));
        }
    }

    [Fact]
    public void FromJustdial_maps_company_to_OrganizationName()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-TEST-4",
            Name = "Company User",
            Mobile = "9820778865",
            Company = "Acme Technologies Ltd"
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        Assert.Equal("Acme Technologies Ltd", lead.OrganizationName);
    }

    [Fact]
    public void FromJustdial_formats_notes_strictly_with_category_and_ext_marker()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-TEST-5",
            Name = "Cat User",
            Mobile = "9820778865",
            Category = "Generator Dealers"
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        var expectedNotes = "Generator Dealers\n[crm-ext:Justdial:JD-TEST-5]";
        Assert.Equal(expectedNotes, lead.Notes);
        Assert.Equal("Generator Dealers", lead.Requirement);
    }

    [Fact]
    public void FromJustdial_formats_notes_with_only_ext_marker_when_category_empty()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-TEST-6",
            Name = "No Cat User",
            Mobile = "9820778865",
            Category = ""
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        var expectedNotes = "[crm-ext:Justdial:JD-TEST-6]";
        Assert.Equal(expectedNotes, lead.Notes);
    }

    [Fact]
    public void FromJustdial_consolidates_address_correctly()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-TEST-7",
            Name = "Address User",
            Mobile = "9820778865",
            Area = "Ghatkopar West",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400086"
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        Assert.Equal("Ghatkopar West, Mumbai, Maharashtra - 400086", lead.Location);
    }

    [Fact]
    public void FromJustdial_consolidates_address_with_brancharea_and_branchpin_fallbacks_when_pincode_is_zero()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-TEST-8",
            Name = "Branch Address User",
            Mobile = "9820778865",
            Area = "",
            Brancharea = "Apollo Bunder",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "0",
            Branchpin = "400001"
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        Assert.Equal("Apollo Bunder, Mumbai, Maharashtra - 400001", lead.Location);
    }

    [Fact]
    public void FromJustdial_serializes_unmapped_metadata_into_raw_payload_json()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JDF99CB59600766",
            Leadtype = "category",
            Prefix = "Mr",
            Name = "Rahul Sharma",
            Mobile = "9820778865",
            Phone = "0221234567",
            Date = "2026-07-21",
            Time = "13:10:11",
            Parentid = "PXX22.XX22.150705230454.M4B",
            Dncmobile = "0",
            Dncphone = "1"
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        Assert.NotNull(lead.RawPayload);
        using var doc = JsonDocument.Parse(lead.RawPayload!);
        var root = doc.RootElement;

        Assert.Equal("category", root.GetProperty("leadtype").GetString());
        Assert.Equal("Mr", root.GetProperty("prefix").GetString());
        Assert.Equal("PXX22.XX22.150705230454.M4B", root.GetProperty("parentid").GetString());
        Assert.Equal("0", root.GetProperty("dncmobile").GetString());
        Assert.Equal("1", root.GetProperty("dncphone").GetString());
        Assert.Equal("2026-07-21", root.GetProperty("raw_date").GetString());
        Assert.Equal("13:10:11", root.GetProperty("raw_time").GetString());
        Assert.Equal("0221234567", root.GetProperty("phone").GetString());
    }

    [Fact]
    public void FromJustdial_splits_name_and_parses_datetime_to_utc()
    {
        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-TEST-9",
            Name = "Rahul Sharma",
            Mobile = "9820778865",
            Date = "2026-07-21",
            Time = "13:10:11"
        };

        var lead = MarketplaceLeadMapper.FromJustdial(dto);

        Assert.Equal("Rahul", lead.FirstName);
        Assert.Equal("Sharma", lead.LastName);
        Assert.NotNull(lead.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, lead.CreatedAt!.Value.Kind);

        // 13:10:11 IST (UTC+5:30) is 07:40:11 UTC
        Assert.Equal(7, lead.CreatedAt.Value.Hour);
        Assert.Equal(40, lead.CreatedAt.Value.Minute);
        Assert.Equal(11, lead.CreatedAt.Value.Second);
    }

    [Fact]
    public async Task MarketplaceLeadPersistenceService_persists_organization_location_and_raw_payload()
    {
        var options = new DbContextOptionsBuilder<TaskDbcontext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new TaskDbcontext(options);
        var roundRobin = new NoOpRoundRobinService();
        var service = new MarketplaceLeadPersistenceService(db, roundRobin, NullLogger<MarketplaceLeadPersistenceService>.Instance);

        var dto = new JustdialWebhookLeadDto
        {
            Leadid = "JD-PERSIST-1",
            Name = "Enterprise Buyer",
            Mobile = "9820778865",
            Company = "Reliance Power",
            Area = "Nariman Point",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400021",
            Category = "Generators",
            Date = "2026-07-21",
            Time = "13:10:11"
        };

        var incoming = MarketplaceLeadMapper.FromJustdial(dto);
        var result = await service.PersistOneAsync(
            MarketplaceLeadMapper.JustdialMarkerName,
            MarketplaceLeadMapper.JustdialLeadSource,
            incoming);

        Assert.Equal(MarketplaceLeadPersistOutcome.Created, result.Outcome);

        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == result.LeadId);
        Assert.NotNull(lead);
        Assert.Equal("Enterprise", lead!.FirstName);
        Assert.Equal("Buyer", lead.LastName);
        Assert.Equal("9820778865", lead.Mobile);
        Assert.Equal("Nariman Point, Mumbai, Maharashtra - 400021", lead.Location);
        Assert.NotNull(lead.RawPayload);
        Assert.NotNull(lead.OrganizationId);

        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == lead.OrganizationId);
        Assert.NotNull(org);
        Assert.Equal("Reliance Power", org!.Name);

        Assert.Equal("Generators\n[crm-ext:Justdial:JD-PERSIST-1]", lead.Notes);
        Assert.NotNull(lead.LeadDate);
        Assert.Equal(DateTimeKind.Utc, lead.LeadDate!.Value.Kind);
    }

    private sealed class NoOpRoundRobinService : ILeadSyncRoundRobinService
    {
        public Task<bool> TryApplyOwnerForSyncLeadAsync(Lead lead, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<int?> PeekNextOwnerIdAsync(int sourceId, CancellationToken cancellationToken = default)
            => Task.FromResult<int?>(null);
    }
}
