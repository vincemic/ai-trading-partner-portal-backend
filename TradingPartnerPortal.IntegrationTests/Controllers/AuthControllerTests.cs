using System.Net;
using FluentAssertions;

namespace TradingPartnerPortal.IntegrationTests.Controllers;

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(TestApplicationFactory factory) : base(factory)
    {
        // Clear authentication for auth tests
        ClearAuthenticationToken();
    }

    [Fact]
    public async Task Health_ReturnsHealthyStatus()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();
        content.Should().Contain("status");
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task Version_ReturnsVersionInformation()
    {
        // Act
        var response = await Client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();
        content.Should().Contain("version");
        content.Should().Contain("build");
        content.Should().Contain("timestamp");
    }
}