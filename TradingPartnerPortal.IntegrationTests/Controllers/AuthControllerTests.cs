using System.Net;
using FluentAssertions;
using TradingPartnerPortal.Infrastructure.Authentication;

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
        content.Should().Contain("version");
        content.Should().Contain("build");
        content.Should().Contain("timestamp");
    }

    [Fact]
    public async Task FakeLogin_WithValidRequest_ReturnsSessionToken()
    {
        // Arrange
        var request = new FakeAuthenticationService.FakeLoginRequest
        {
            UserId = "test-user",
            PartnerId = Guid.NewGuid().ToString(),
            Role = "PartnerUser"
        };

        // Act
        var response = await Client.PostAsync("/api/fake-login", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginResponse = await GetResponseContentAsync<FakeAuthenticationService.FakeLoginResponse>(response);
        loginResponse.Should().NotBeNull();
        loginResponse.SessionToken.Should().NotBeEmpty();
        loginResponse.UserId.Should().Be(request.UserId);
        loginResponse.PartnerId.Should().Be(request.PartnerId);
        loginResponse.Role.Should().Be(request.Role);
        loginResponse.ExpiresAt.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FakeLogin_WithPartnerAdminRole_ReturnsSessionToken()
    {
        // Arrange
        var request = new FakeAuthenticationService.FakeLoginRequest
        {
            UserId = "admin-user",
            PartnerId = Guid.NewGuid().ToString(),
            Role = "PartnerAdmin"
        };

        // Act
        var response = await Client.PostAsync("/api/fake-login", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginResponse = await GetResponseContentAsync<FakeAuthenticationService.FakeLoginResponse>(response);
        loginResponse.Role.Should().Be("PartnerAdmin");
    }

    [Fact]
    public async Task FakeLogin_WithInternalSupportRole_ReturnsSessionToken()
    {
        // Arrange
        var request = new FakeAuthenticationService.FakeLoginRequest
        {
            UserId = "support-user",
            PartnerId = Guid.NewGuid().ToString(),
            Role = "InternalSupport"
        };

        // Act
        var response = await Client.PostAsync("/api/fake-login", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginResponse = await GetResponseContentAsync<FakeAuthenticationService.FakeLoginResponse>(response);
        loginResponse.Role.Should().Be("InternalSupport");
    }

    [Fact]
    public async Task FakeLogin_WithInvalidRole_ReturnsBadRequest()
    {
        // Arrange
        var request = new FakeAuthenticationService.FakeLoginRequest
        {
            UserId = "test-user",
            PartnerId = Guid.NewGuid().ToString(),
            Role = "InvalidRole"
        };

        // Act
        var response = await Client.PostAsync("/api/fake-login", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid role");
    }

    [Fact]
    public async Task FakeLogin_WithInvalidPartnerId_ReturnsBadRequest()
    {
        // Arrange
        var request = new FakeAuthenticationService.FakeLoginRequest
        {
            UserId = "test-user",
            PartnerId = "invalid-guid",
            Role = "PartnerUser"
        };

        // Act
        var response = await Client.PostAsync("/api/fake-login", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid partner ID format");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task FakeLogin_WithEmptyUserId_ReturnsBadRequest(string? userId)
    {
        // Arrange
        var request = new FakeAuthenticationService.FakeLoginRequest
        {
            UserId = userId ?? string.Empty,
            PartnerId = Guid.NewGuid().ToString(),
            Role = "PartnerUser"
        };

        // Act
        var response = await Client.PostAsync("/api/fake-login", CreateJsonContent(request));

        // Assert
        // The validation might be handled differently, but we expect some form of error
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task FakeLogin_CreatesValidSessionToken()
    {
        // Arrange
        var partnerId = Guid.NewGuid().ToString();
        var request = new FakeAuthenticationService.FakeLoginRequest
        {
            UserId = "test-user",
            PartnerId = partnerId,
            Role = "PartnerUser"
        };

        // Act
        var loginResponse = await Client.PostAsync("/api/fake-login", CreateJsonContent(request));
        var login = await GetResponseContentAsync<FakeAuthenticationService.FakeLoginResponse>(loginResponse);

        // Use the session token for another request
        SetAuthenticationToken(login.SessionToken);
        var healthResponse = await Client.GetAsync("/api/health");

        // Assert
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}