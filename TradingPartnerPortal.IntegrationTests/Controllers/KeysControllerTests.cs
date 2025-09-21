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
    private readonly Guid _testKeyId = Guid.NewGuid();

    public KeysControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    protected override async Task SeedTestDataAsync()
    {
        // Set admin authentication by default for seeding
        SetAdminAuthentication();

        // Seed test data
        await Factory.SeedTestDataAsync(context =>
        {
            // Add test partner
            var partner = new Partner
            {
                PartnerId = _testPartnerId,
                Name = "Test Partner",
                Status = PartnerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };
            context.Partners.Add(partner);

            // Add test PGP key
            var pgpKey = new PgpKey
            {
                KeyId = _testKeyId,
                PartnerId = _testPartnerId,
                Fingerprint = "1234567890ABCDEF1234567890ABCDEF12345678",
                Algorithm = "RSA",
                KeySize = 2048,
                PublicKeyArmored = "-----BEGIN PGP PUBLIC KEY BLOCK-----\n\nTest public key content\n\n-----END PGP PUBLIC KEY BLOCK-----",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ValidFrom = DateTime.UtcNow.AddDays(-10),
                ValidTo = DateTime.UtcNow.AddDays(365),
                Status = PgpKeyStatus.Active,
                IsPrimary = true
            };
            context.PgpKeys.Add(pgpKey);

            // Add a second key (not primary)
            var secondKey = new PgpKey
            {
                KeyId = Guid.NewGuid(),
                PartnerId = _testPartnerId,
                Fingerprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12",
                Algorithm = "RSA",
                KeySize = 4096,
                PublicKeyArmored = "-----BEGIN PGP PUBLIC KEY BLOCK-----\n\nTest public key content 2\n\n-----END PGP PUBLIC KEY BLOCK-----",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ValidFrom = DateTime.UtcNow.AddDays(-5),
                ValidTo = DateTime.UtcNow.AddDays(365),
                Status = PgpKeyStatus.Active,
                IsPrimary = false
            };
            context.PgpKeys.Add(secondKey);
        });
    }

    [Fact]
    public async Task ListKeys_WithValidSession_ReturnsKeys()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var keys = await GetResponseContentAsync<List<KeySummaryDto>>(response);
        keys.Should().NotBeNull();
        keys.Should().HaveCountGreaterOrEqualTo(1);
        
        var primaryKey = keys.FirstOrDefault(k => k.IsPrimary);
        primaryKey.Should().NotBeNull();
        primaryKey!.KeyId.Should().Be(_testKeyId.ToString());
    }

    [Fact]
    public async Task ListKeys_WithRegularUserSession_ReturnsKeys()
    {
        // Arrange
        await SeedTestDataAsync();
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
        await SeedTestDataAsync();
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
        await SeedTestDataAsync();
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
        await SeedTestDataAsync();
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
        await SeedTestDataAsync();
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
        await SeedTestDataAsync();
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
        await SeedTestDataAsync();
        var request = new RevokeKeyRequest
        {
            Reason = "Test revocation"
        };

        // Act
        var response = await Client.PostAsync($"/api/keys/{_testKeyId}/revoke", CreateJsonContent(request));

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
        await SeedTestDataAsync();
        SetUserAuthentication();
        
        var request = new RevokeKeyRequest
        {
            Reason = "Test revocation"
        };

        // Act
        var response = await Client.PostAsync($"/api/keys/{_testKeyId}/revoke", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeKey_WithNonexistentKey_ReturnsNotFound()
    {
        // Arrange
        await SeedTestDataAsync();
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
        await SeedTestDataAsync();
        
        // Get a non-primary key to promote
        var keysResponse = await Client.GetAsync("/api/keys");
        var keys = await GetResponseContentAsync<List<KeySummaryDto>>(keysResponse);
        var nonPrimaryKey = keys.FirstOrDefault(k => !k.IsPrimary);
        
        if (nonPrimaryKey == null)
        {
            // Skip test if no non-primary key exists
            return;
        }

        // Act
        var response = await Client.PostAsync($"/api/keys/{nonPrimaryKey.KeyId}/promote", CreateJsonContent(new { }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("success");
    }

    [Fact]
    public async Task PromoteKey_WithRegularUserRole_ReturnsForbidden()
    {
        // Arrange
        await SeedTestDataAsync();
        SetUserAuthentication();

        // Act
        var response = await Client.PostAsync($"/api/keys/{_testKeyId}/promote", CreateJsonContent(new { }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PromoteKey_WithNonexistentKey_ReturnsNotFound()
    {
        // Arrange
        await SeedTestDataAsync();
        var nonexistentKeyId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/keys/{nonexistentKeyId}/promote", CreateJsonContent(new { }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PromoteKey_WithAlreadyPrimaryKey_ReturnsSuccessMessage()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.PostAsync($"/api/keys/{_testKeyId}/promote", CreateJsonContent(new { }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("success");
    }
}
