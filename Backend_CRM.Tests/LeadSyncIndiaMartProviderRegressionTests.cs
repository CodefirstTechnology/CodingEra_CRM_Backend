using System.Net;
using System.Text.Json;
using CRM.Configuration;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backend_CRM.Tests
{
    public class LeadSyncIndiaMartProviderRegressionTests
    {
        [Fact]
        public void BuildIndiaMartPullUrl_correctly_constructs_v2_url_with_key_and_ist_range()
        {
            var credentials = new LeadSyncResolvedCredentials
            {
                PullApiUrl = "https://mapi.indiamart.com/wservce/crm/crmListing/v2",
                ApiKey = "sample_glusr_crm_key_123"
            };

            var url = LeadSyncPullHelpers.BuildIndiaMartPullUrl(credentials);

            Assert.Contains("https://mapi.indiamart.com/wservce/crm/crmListing/v2", url);
            Assert.Contains("glusr_crm_key=sample_glusr_crm_key_123", url);
            Assert.Contains("start_time=", url);
            Assert.Contains("end_time=", url);
        }

        [Fact]
        public void GetIndiaMartPullTimeRange_returns_valid_ist_timestamps_within_7_days()
        {
            var (startTime, endTime) = LeadSyncPullHelpers.GetIndiaMartPullTimeRange(7);

            Assert.NotEmpty(startTime);
            Assert.NotEmpty(endTime);
            Assert.Contains(":", startTime);
            Assert.Contains(":", endTime);
        }

        [Fact]
        public void TryGetIndiaMartError_treats_code_204_as_no_leads_not_hard_error()
        {
            var json = """
            {
              "STATUS": "SUCCESS",
              "CODE": 204,
              "MESSAGE": "No records found matching query parameters."
            }
            """;

            using var doc = JsonDocument.Parse(json);
            var err = LeadSyncPullHelpers.TryGetIndiaMartError(doc.RootElement);

            Assert.Null(err); // Code 204 is handled as zero records, not an exception
        }

        [Fact]
        public void MapGenericMarketplaceRow_maps_indiamart_v2_fields_correctly()
        {
            var json = """
            {
              "UNIQUE_QUERY_ID": "IM-PULL-7788",
              "SENDER_NAME": "Harish Verma",
              "SENDER_MOBILE": "9898989898",
              "SENDER_EMAIL": "harish@example.com",
              "SUBJECT": "Bulk Steel Pipes",
              "QUERY_MESSAGE": "Require 1000m seamless pipes.",
              "GLUSR_USR_COMPANYNAME": "Verma Steel Corp",
              "SENDER_CITY": "Ahmedabad"
            }
            """;

            using var doc = JsonDocument.Parse(json);
            var mapped = LeadSyncPullHelpers.MapGenericMarketplaceRow(doc.RootElement, "IndiaMART", "IndiaMART");

            Assert.NotNull(mapped);
            Assert.Equal("IM-PULL-7788", mapped.ExternalKey);
            Assert.Equal("Harish", mapped.FirstName);
            Assert.Equal("Verma", mapped.LastName);
            Assert.Equal("9898989898", mapped.Mobile);
            Assert.Equal("harish@example.com", mapped.Email);
            Assert.Equal("Verma Steel Corp", mapped.OrganizationName);
            Assert.Contains("[crm-ext:IndiaMART:IM-PULL-7788]", mapped.Notes);
            Assert.Contains("Company: Verma Steel Corp", mapped.Notes);
            Assert.Contains("City: Ahmedabad", mapped.Notes);
        }
    }
}
