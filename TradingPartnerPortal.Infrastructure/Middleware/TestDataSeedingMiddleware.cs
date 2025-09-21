using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Domain.Enums;
using TradingPartnerPortal.Infrastructure.Data;

namespace TradingPartnerPortal.Infrastructure.Middleware;

/// <summary>
/// Middleware that automatically seeds test data when in development or testing environment.
/// This ensures consistent test data is available across all HTTP requests.
/// </summary>
public class TestDataSeedingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TestDataSeedingMiddleware> _logger;
    private static bool _isSeeded = false;
    private static readonly object _seedLock = new();

    public TestDataSeedingMiddleware(
        RequestDelegate next, 
        ILogger<TestDataSeedingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only seed if not already seeded
        if (!_isSeeded)
        {
            await SeedTestDataAsync(context);
        }

        await _next(context);
    }

    private async Task SeedTestDataAsync(HttpContext context)
    {
        lock (_seedLock)
        {
            if (_isSeeded) return;
            _isSeeded = true;
        }

        try
        {
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TradingPartnerPortalDbContext>();

            _logger.LogInformation("Seeding test data for testing environment");

            // Check if data already exists
            if (dbContext.Partners.Any())
            {
                _logger.LogInformation("Test data already exists, skipping seeding");
                return;
            }

            // Seed test partners
            var testPartnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var testPartner = new Partner
            {
                PartnerId = testPartnerId,
                Name = "Test Partner",
                Status = PartnerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };
            dbContext.Partners.Add(testPartner);

            // Seed additional test partner for isolation testing
            var secondPartnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var secondPartner = new Partner
            {
                PartnerId = secondPartnerId,
                Name = "Second Test Partner",
                Status = PartnerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            };
            dbContext.Partners.Add(secondPartner);

            // Seed PGP keys for the main test partner
            var primaryKeyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var primaryKey = new PgpKey
            {
                KeyId = primaryKeyId,
                PartnerId = testPartnerId,
                Fingerprint = "1234567890ABCDEF1234567890ABCDEF12345678",
                Algorithm = "RSA",
                KeySize = 2048,
                PublicKeyArmored = "-----BEGIN PGP PUBLIC KEY BLOCK-----\n\nTest primary public key content\n\n-----END PGP PUBLIC KEY BLOCK-----",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ValidFrom = DateTime.UtcNow.AddDays(-10),
                ValidTo = DateTime.UtcNow.AddDays(365),
                Status = PgpKeyStatus.Active,
                IsPrimary = true
            };
            dbContext.PgpKeys.Add(primaryKey);

            // Add a secondary (non-primary) key
            var secondaryKeyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var secondaryKey = new PgpKey
            {
                KeyId = secondaryKeyId,
                PartnerId = testPartnerId,
                Fingerprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12",
                Algorithm = "RSA",
                KeySize = 4096,
                PublicKeyArmored = "-----BEGIN PGP PUBLIC KEY BLOCK-----\n\nTest secondary public key content\n\n-----END PGP PUBLIC KEY BLOCK-----",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ValidFrom = DateTime.UtcNow.AddDays(-5),
                ValidTo = DateTime.UtcNow.AddDays(365),
                Status = PgpKeyStatus.Active,
                IsPrimary = false
            };
            dbContext.PgpKeys.Add(secondaryKey);

            // Seed SFTP credentials
            var sftpCredential = new SftpCredential
            {
                PartnerId = testPartnerId,
                PasswordHash = "hashed-test-password", // This would be properly hashed in real implementation
                PasswordSalt = "test-salt",
                LastRotatedAt = DateTime.UtcNow.AddDays(-10),
                RotationMethod = PasswordRotationMethod.Manual
            };
            dbContext.SftpCredentials.Add(sftpCredential);

            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded test data: {PartnerCount} partners, {KeyCount} keys, {CredentialCount} SFTP credentials",
                2, 2, 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding test data");
            // Reset the flag so we can try again on the next request
            lock (_seedLock)
            {
                _isSeeded = false;
            }
            throw;
        }
    }
}