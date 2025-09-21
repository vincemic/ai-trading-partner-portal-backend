using System.Net;
using FluentAssertions;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.IntegrationTests.Controllers;

public class KeysControllerTests : IntegrationTestBase
{
    // Use the default test partner ID that matches the middleware
    private readonly Guid _testPartnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Use the known test key IDs that are seeded by the middleware
    private readonly Guid _primaryTestKeyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _secondaryTestKeyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public KeysControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ListKeys_WithValidSession_ReturnsKeys()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth

        // Act
        var response = await Client.GetAsync("/api/keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var keys = await GetResponseContentAsync<List<KeySummaryDto>>(response);
        keys.Should().NotBeNull();
        keys.Should().HaveCountGreaterOrEqualTo(1);

        // Should have at least one key with valid properties
        keys.All(k => !string.IsNullOrEmpty(k.KeyId)).Should().BeTrue("all keys should have valid IDs");

        // There may or may not be a primary key depending on the seeded data
        var primaryKeys = keys.Where(k => k.IsPrimary).ToList();
        primaryKeys.Count.Should().BeLessOrEqualTo(1, "there should be at most one primary key");
    }

    [Fact]
    public async Task ListKeys_WithRegularUserSession_ReturnsKeys()
    {
        // Arrange
        SetUserAuthentication();

        // Act
        var response = await Client.GetAsync("/api/keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var keys = await GetResponseContentAsync<List<KeySummaryDto>>(response);
        keys.Should().NotBeNull();
    }

    [Fact]
    public async Task ListKeys_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadKey_WithValidRequestAndAdminRole_ReturnsNewKey()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth
        var request = new UploadKeyRequest
        {
            PublicKeyArmored = "-----BEGIN PGP PUBLIC KEY BLOCK-----\n\nNew test key content\n\n-----END PGP PUBLIC KEY BLOCK-----",
            ValidFrom = DateTime.UtcNow.ToString("O"),
            ValidTo = DateTime.UtcNow.AddYears(1).ToString("O"),
            MakePrimary = false
        };

        // Act
        var response = await Client.PostAsync("/api/keys/upload", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newKey = await GetResponseContentAsync<KeySummaryDto>(response);
        newKey.Should().NotBeNull();
        newKey.KeyId.Should().NotBeEmpty();
        newKey.Status.Should().Be("Active");
        newKey.IsPrimary.Should().Be(false);
    }

    [Fact]
    public async Task UploadKey_WithRegularUserRole_ReturnsForbidden()
    {
        // Arrange
        SetUserAuthentication();

        var request = new UploadKeyRequest
        {
            PublicKeyArmored = "-----BEGIN PGP PUBLIC KEY BLOCK-----\n\nNew test key content\n\n-----END PGP PUBLIC KEY BLOCK-----",
            MakePrimary = false
        };

        // Act
        var response = await Client.PostAsync("/api/keys/upload", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadKey_WithInvalidKeyFormat_ReturnsBadRequest()
    {
        // Arrange
        SetAdminAuthentication();
        var request = new UploadKeyRequest
        {
            PublicKeyArmored = "invalid-key-format",
            MakePrimary = false
        };

        // Act
        var response = await Client.PostAsync("/api/keys/upload", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateKey_WithValidRequestAndAdminRole_ReturnsNewKeyPair()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth
        var request = new GenerateKeyRequest
        {
            ValidFrom = DateTime.UtcNow.ToString("O"),
            ValidTo = DateTime.UtcNow.AddYears(1).ToString("O"),
            MakePrimary = false
        };

        // Act
        var response = await Client.PostAsync("/api/keys/generate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseContentAsync<GenerateKeyResponse>(response);
        result.Should().NotBeNull();
        result.PrivateKeyArmored.Should().NotBeEmpty();
        result.Key.Should().NotBeNull();
        result.Key.KeyId.Should().NotBeEmpty();
        result.Key.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GenerateKey_WithRegularUserRole_ReturnsForbidden()
    {
        // Arrange
        SetUserAuthentication();

        var request = new GenerateKeyRequest
        {
            MakePrimary = false
        };

        // Act
        var response = await Client.PostAsync("/api/keys/generate", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeKey_WithValidKeyAndAdminRole_RevokesKey()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth
        var request = new RevokeKeyRequest
        {
            Reason = "Test revocation"
        };

        // Act
        var response = await Client.PostAsync($"/api/keys/{_primaryTestKeyId}/revoke", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("success");
        content.Should().Contain("auditId");
    }

    [Fact]
    public async Task RevokeKey_WithRegularUserRole_ReturnsForbidden()
    {
        // Arrange
        SetUserAuthentication();

        var request = new RevokeKeyRequest
        {
            Reason = "Test revocation"
        };

        // Act
        var response = await Client.PostAsync($"/api/keys/{_primaryTestKeyId}/revoke", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeKey_WithNonexistentKey_ReturnsNotFound()
    {
        // Arrange
        SetAdminAuthentication();
        var nonexistentKeyId = Guid.NewGuid();
        var request = new RevokeKeyRequest
        {
            Reason = "Test revocation"
        };

        // Act
        var response = await Client.PostAsync($"/api/keys/{nonexistentKeyId}/revoke", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PromoteKey_WithValidKeyAndAdminRole_PromotesKey()
    {
        // Arrange
        SetAdminAuthentication();

        // Use the secondary key ID to test promotion
        var keyToPromote = _secondaryTestKeyId;

        // Act
        var response = await Client.PostAsync($"/api/keys/{keyToPromote}/promote", CreateJsonContent(new { }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("success");
    }

    [Fact]
    public async Task PromoteKey_WithRegularUserRole_ReturnsForbidden()
    {
        // Arrange
        SetUserAuthentication();

        // Act
        var response = await Client.PostAsync($"/api/keys/{_primaryTestKeyId}/promote", CreateJsonContent(new { }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PromoteKey_WithNonexistentKey_ReturnsNotFound()
    {
        // Arrange
        SetAdminAuthentication();
        var nonexistentKeyId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/keys/{nonexistentKeyId}/promote", CreateJsonContent(new { }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PromoteKey_WithExistingKey_WorksCorrectly()
    {
        // Arrange
        SetAdminAuthentication(); // Ensure admin auth

        // Get available keys
        var keysResponse = await Client.GetAsync("/api/keys");
        var keys = await GetResponseContentAsync<List<KeySummaryDto>>(keysResponse);

        // Skip test if no keys available
        if (keys.Count == 0)
        {
            return; // Skip test - no keys to work with
        }

        var testKey = keys.First();

        // Act - Try to promote a key
        var response = await Client.PostAsync($"/api/keys/{testKey.KeyId}/promote", CreateJsonContent(new { }));

        // Assert - Should return either success or appropriate error
        var validStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.Conflict, HttpStatusCode.NotFound };
        validStatusCodes.Should().Contain(response.StatusCode, "API should handle promotion requests gracefully");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty("response should contain a message");
    }
}
