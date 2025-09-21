using System.Net;
using FluentAssertions;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.IntegrationTests.Controllers;

public class SftpControllerTests : IntegrationTestBase
{
    // Use the default test partner ID that matches the middleware
    private readonly Guid _testPartnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public SftpControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCredentialMetadata_WithValidSession_ReturnsMetadata()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth

        // Act
        var response = await Client.GetAsync("/api/sftp/credential");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var metadata = await GetResponseContentAsync<SftpCredentialMetadataDto>(response);
        metadata.Should().NotBeNull();
        metadata.LastRotatedAt.Should().NotBeNullOrEmpty();
        metadata.RotationMethod.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCredentialMetadata_WithRegularUserSession_ReturnsMetadata()
    {
        // Arrange
        SetUserAuthentication(); // Ensure user auth

        // Act
        var response = await Client.GetAsync("/api/sftp/credential");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var metadata = await GetResponseContentAsync<SftpCredentialMetadataDto>(response);
        metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCredentialMetadata_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/sftp/credential");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCredentialMetadata_WithNonexistentCredential_ReturnsNotFound()
    {
        // Arrange - Create a different partner context by using a custom token
        // This simulates a partner with no SFTP credentials
        SetAuthenticationToken("test-admin-different-partner");

        // Act
        var response = await Client.GetAsync("/api/sftp/credential");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RotatePassword_WithManualModeAndAdminRole_ReturnsNewPassword()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth
        var request = new RotatePasswordRequest
        {
            Mode = "manual",
            NewPassword = "new-secure-password-123!"
        };

        // Act
        var response = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseContentAsync<RotatePasswordResponse>(response);
        result.Should().NotBeNull();
        result.Password.Should().Be(request.NewPassword);
        result.Metadata.Should().NotBeNull();
        result.Metadata.LastRotatedAt.Should().NotBeNullOrEmpty();
        result.Metadata.RotationMethod.Should().Be("manual");
    }

    [Fact]
    public async Task RotatePassword_WithAutoModeAndAdminRole_ReturnsGeneratedPassword()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth
        var request = new RotatePasswordRequest
        {
            Mode = "auto"
        };

        // Act
        var response = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseContentAsync<RotatePasswordResponse>(response);
        result.Should().NotBeNull();
        result.Password.Should().NotBeNullOrEmpty();
        result.Password.Should().NotBe(request.NewPassword); // Should be auto-generated
        result.Metadata.Should().NotBeNull();
        result.Metadata.RotationMethod.Should().Be("auto");
    }

    [Fact]
    public async Task RotatePassword_WithRegularUserRole_ReturnsForbidden()
    {
        // Arrange
        SetUserAuthentication();

        var request = new RotatePasswordRequest
        {
            Mode = "manual",
            NewPassword = "new-password"
        };

        // Act
        var response = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RotatePassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationToken();
        var request = new RotatePasswordRequest
        {
            Mode = "manual",
            NewPassword = "new-password"
        };

        // Act
        var response = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RotatePassword_WithInvalidMode_ReturnsBadRequest()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth
        var request = new RotatePasswordRequest
        {
            Mode = "invalid-mode",
            NewPassword = "new-password"
        };

        // Act
        var response = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RotatePassword_WithManualModeButNoPassword_ReturnsBadRequest()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth
        var request = new RotatePasswordRequest
        {
            Mode = "manual"
            // NewPassword intentionally not set
        };

        // Act
        var response = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")] // Too short
    [InlineData("password")] // Too simple
    public async Task RotatePassword_WithWeakPassword_ReturnsBadRequest(string weakPassword)
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth
        var request = new RotatePasswordRequest
        {
            Mode = "manual",
            NewPassword = weakPassword
        };

        // Act
        var response = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RotatePassword_WithEmptyMode_ReturnsBadRequest()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth
        var request = new RotatePasswordRequest
        {
            Mode = "",
            NewPassword = "valid-password-123!"
        };

        // Act
        var response = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RotatePassword_MultipleRotations_UpdatesMetadata()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth

        // First rotation
        var firstRequest = new RotatePasswordRequest
        {
            Mode = "manual",
            NewPassword = "first-password-123!"
        };

        var firstResponse = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(firstRequest));
        var firstResult = await GetResponseContentAsync<RotatePasswordResponse>(firstResponse);
        var firstRotationTime = firstResult.Metadata.LastRotatedAt;

        // Wait a moment to ensure different timestamps
        await Task.Delay(1000);

        // Second rotation
        var secondRequest = new RotatePasswordRequest
        {
            Mode = "auto"
        };

        // Act
        var secondResponse = await Client.PostAsync("/api/sftp/credential/rotate", CreateJsonContent(secondRequest));

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResult = await GetResponseContentAsync<RotatePasswordResponse>(secondResponse);
        secondResult.Metadata.LastRotatedAt.Should().NotBe(firstRotationTime);
        secondResult.Metadata.RotationMethod.Should().Be("auto");
        secondResult.Password.Should().NotBe(firstRequest.NewPassword);
    }
}
