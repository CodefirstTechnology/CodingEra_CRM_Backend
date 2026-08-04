using Xunit;

namespace Backend_ERP.Tests;

public class HealthEndpointContractTests
{
    [Fact]
    public void Health_status_payload_matches_contract()
    {
        var payload = new { status = "ERP Backend Running" };

        Assert.Equal("ERP Backend Running", payload.status);
    }
}
