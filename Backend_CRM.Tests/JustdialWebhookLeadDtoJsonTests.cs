using System.Text.Json;
using CRM.DTO;
using Xunit;

namespace Backend_CRM.Tests;

public class JustdialWebhookLeadDtoJsonTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    public void Deserializes_dncmobile_as_number(int jsonValue, string expected)
    {
        var json = MinimalPayload($@"""dncmobile"":{jsonValue}");
        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(expected, dto!.Dncmobile);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void Deserializes_dncmobile_as_string(string jsonValue)
    {
        var json = MinimalPayload($@"""dncmobile"":""{jsonValue}""");
        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(jsonValue, dto!.Dncmobile);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    public void Deserializes_dncphone_as_number(int jsonValue, string expected)
    {
        var json = MinimalPayload($@"""dncphone"":{jsonValue}");
        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(expected, dto!.Dncphone);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void Deserializes_dncphone_as_string(string jsonValue)
    {
        var json = MinimalPayload($@"""dncphone"":""{jsonValue}""");
        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(jsonValue, dto!.Dncphone);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(411038, "411038")]
    public void Deserializes_pincode_as_number(int jsonValue, string expected)
    {
        var json = MinimalPayload($@"""pincode"":{jsonValue}");
        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(expected, dto!.Pincode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("411038")]
    public void Deserializes_pincode_as_string(string jsonValue)
    {
        var json = MinimalPayload($@"""pincode"":""{jsonValue}""");
        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(jsonValue, dto!.Pincode);
    }

    [Theory]
    [InlineData(400001, "400001")]
    [InlineData(0, "0")]
    public void Deserializes_branchpin_as_number(int jsonValue, string expected)
    {
        var json = MinimalPayload($@"""branchpin"":{jsonValue}");
        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(expected, dto!.Branchpin);
    }

    [Theory]
    [InlineData("400001")]
    [InlineData("0")]
    public void Deserializes_branchpin_as_string(string jsonValue)
    {
        var json = MinimalPayload($@"""branchpin"":""{jsonValue}""");
        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(jsonValue, dto!.Branchpin);
    }

    [Fact]
    public void Deserializes_production_justdial_payload_with_numeric_dnc_and_state()
    {
        const string json = """
            {
              "leadid": "JDF99CB59600766",
              "leadtype": "category",
              "prefix": "",
              "name": "Messaging6",
              "mobile": "9820778865",
              "phone": "",
              "email": "",
              "date": "2026-07-21",
              "category": "Generator Dealers",
              "area": "Ghatkopar West",
              "city": "Mumbai",
              "brancharea": "Apollo Bunder",
              "dncmobile": 0,
              "dncphone": 0,
              "company": "Greaves Ltd",
              "pincode": "0",
              "time": "13:10:11",
              "branchpin": "400001",
              "parentid": "PXX22.XX22.150705230454.M4B",
              "state": "Maharashtra"
            }
            """;

        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);

        Assert.NotNull(dto);
        Assert.Equal("JDF99CB59600766", dto!.Leadid);
        Assert.Equal("Messaging6", dto.Name);
        Assert.Equal("9820778865", dto.Mobile);
        Assert.Equal("0", dto.Dncmobile);
        Assert.Equal("0", dto.Dncphone);
        Assert.Equal("0", dto.Pincode);
        Assert.Equal("400001", dto.Branchpin);
        Assert.Equal("Maharashtra", dto.State);
        Assert.Equal("Generator Dealers", dto.Category);
        Assert.Equal("Mumbai", dto.City);
    }

    [Fact]
    public void Deserializes_payload_with_numeric_pincode_and_branchpin()
    {
        const string json = """
            {
              "leadid": "JD-NUM-PIN",
              "name": "Pin Test",
              "mobile": "9999999999",
              "dncmobile": 1,
              "dncphone": 1,
              "pincode": 411038,
              "branchpin": 411001,
              "state": "Maharashtra",
              "unknownFutureField": "ignored"
            }
            """;

        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);

        Assert.NotNull(dto);
        Assert.Equal("1", dto!.Dncmobile);
        Assert.Equal("1", dto.Dncphone);
        Assert.Equal("411038", dto.Pincode);
        Assert.Equal("411001", dto.Branchpin);
        Assert.Equal("Maharashtra", dto.State);
    }

    [Fact]
    public void Deserializes_legacy_string_dnc_payload()
    {
        const string json = """
            {
              "leadid": "PRELIVE-001",
              "name": "Test User",
              "mobile": "9876543210",
              "dncmobile": "0",
              "dncphone": "0",
              "pincode": "411038",
              "branchpin": "411001"
            }
            """;

        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);

        Assert.NotNull(dto);
        Assert.Equal("0", dto!.Dncmobile);
        Assert.Equal("0", dto.Dncphone);
        Assert.Equal("411038", dto.Pincode);
        Assert.Equal("411001", dto.Branchpin);
    }

    [Fact]
    public void Ignores_unknown_additional_fields()
    {
        const string json = """
            {
              "leadid": "JD-EXTRA",
              "name": "Extra Fields",
              "mobile": "8888888888",
              "state": "Maharashtra",
              "district": "Mumbai Suburban",
              "campaignid": "CAMP-1"
            }
            """;

        var dto = JsonSerializer.Deserialize<JustdialWebhookLeadDto>(json, Options);

        Assert.NotNull(dto);
        Assert.Equal("JD-EXTRA", dto!.Leadid);
        Assert.Equal("Maharashtra", dto.State);
    }

    private static string MinimalPayload(string extraFieldJson) =>
        $@"{{""leadid"":""JD1"",""name"":""A"",""mobile"":""1"",{extraFieldJson}}}";
}
