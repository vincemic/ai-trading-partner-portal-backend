using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Domain.Enums;
using TradingPartnerPortal.Infrastructure.Data;

namespace TradingPartnerPortal.Infrastructure.Services;

public class MockDataSeeder : IMockDataSeeder
{
    private readonly TradingPartnerPortalDbContext _context;
    private readonly Random _random = new();

    public MockDataSeeder(TradingPartnerPortalDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Check if data already exists
        if (_context.Partners.Any())
        {
            return; // Already seeded
        }

        // Create sample partners
        var partners = CreateSamplePartners();
        await _context.Partners.AddRangeAsync(partners);
        await _context.SaveChangesAsync();

        // Create sample keys for each partner
        var keys = CreateSampleKeys(partners);
        await _context.PgpKeys.AddRangeAsync(keys);
        await _context.SaveChangesAsync();

        // Create sample SFTP credentials
        var credentials = CreateSampleSftpCredentials(partners);
        await _context.SftpCredentials.AddRangeAsync(credentials);
        await _context.SaveChangesAsync();

        // Create sample file transfer events
        var fileEvents = CreateSampleFileEvents(partners);
        await _context.FileTransferEvents.AddRangeAsync(fileEvents);
        await _context.SaveChangesAsync();

        // Create sample connection events
        var connectionEvents = CreateSampleConnectionEvents(partners);
        await _context.SftpConnectionEvents.AddRangeAsync(connectionEvents);
        await _context.SaveChangesAsync();

        // Create sample audit events
        var auditEvents = CreateSampleAuditEvents(partners);
        await _context.AuditEvents.AddRangeAsync(auditEvents);
        await _context.SaveChangesAsync();
    }

    private List<Partner> CreateSamplePartners()
    {
        var partners = new List<Partner>
        {
            new()
            {
                PartnerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Acme Corporation",
                Status = PartnerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            },
            new()
            {
                PartnerId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Global Logistics Inc",
                Status = PartnerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-75)
            },
            new()
            {
                PartnerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "TechFlow Systems",
                Status = PartnerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new()
            {
                PartnerId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "MegaTrade Ltd",
                Status = PartnerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-45)
            },
            new()
            {
                PartnerId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "DataSync Partners",
                Status = PartnerStatus.Suspended,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            }
        };

        return partners;
    }

    private List<PgpKey> CreateSampleKeys(List<Partner> partners)
    {
        var keys = new List<PgpKey>();

        foreach (var partner in partners)
        {
            // Primary active key
            keys.Add(new PgpKey
            {
                KeyId = Guid.NewGuid(),
                PartnerId = partner.PartnerId,
                PublicKeyArmored = GenerateMockPgpKey($"primary-{partner.Name}"),
                Fingerprint = GenerateMockFingerprint(),
                Algorithm = "RSA",
                KeySize = 4096,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                ValidFrom = DateTime.UtcNow.AddDays(-30),
                ValidTo = DateTime.UtcNow.AddDays(335),
                Status = PgpKeyStatus.Active,
                IsPrimary = true
            });

            // Secondary active key (for overlap scenarios)
            keys.Add(new PgpKey
            {
                KeyId = Guid.NewGuid(),
                PartnerId = partner.PartnerId,
                PublicKeyArmored = GenerateMockPgpKey($"secondary-{partner.Name}"),
                Fingerprint = GenerateMockFingerprint(),
                Algorithm = "RSA",
                KeySize = 4096,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ValidFrom = DateTime.UtcNow.AddDays(-10),
                ValidTo = DateTime.UtcNow.AddDays(355),
                Status = PgpKeyStatus.Active,
                IsPrimary = false
            });

            // Revoked old key
            if (_random.NextDouble() > 0.5)
            {
                keys.Add(new PgpKey
                {
                    KeyId = Guid.NewGuid(),
                    PartnerId = partner.PartnerId,
                    PublicKeyArmored = GenerateMockPgpKey($"old-{partner.Name}"),
                    Fingerprint = GenerateMockFingerprint(),
                    Algorithm = "RSA",
                    KeySize = 2048,
                    CreatedAt = DateTime.UtcNow.AddDays(-120),
                    ValidFrom = DateTime.UtcNow.AddDays(-120),
                    ValidTo = DateTime.UtcNow.AddDays(-30),
                    RevokedAt = DateTime.UtcNow.AddDays(-30),
                    Status = PgpKeyStatus.Revoked,
                    IsPrimary = false
                });
            }
        }

        return keys;
    }

    private List<SftpCredential> CreateSampleSftpCredentials(List<Partner> partners)
    {
        var credentials = new List<SftpCredential>();

        foreach (var partner in partners)
        {
            credentials.Add(new SftpCredential
            {
                PartnerId = partner.PartnerId,
                PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$abcdefghijklmnop$xyz123...", // Mock hash
                PasswordSalt = "mocksalt123",
                LastRotatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 90)),
                RotationMethod = _random.NextDouble() > 0.5 ? PasswordRotationMethod.Auto : PasswordRotationMethod.Manual
            });
        }

        return credentials;
    }

    private List<FileTransferEvent> CreateSampleFileEvents(List<Partner> partners)
    {
        var fileEvents = new List<FileTransferEvent>();
        var docTypes = new[] { "850", "810", "997", "856", "832", "204" };
        var statuses = new[] { FileStatus.Success, FileStatus.Failed, FileStatus.Processing, FileStatus.Pending };
        var errorCodes = new[] { "PARSE_ERROR", "VALIDATION_FAILED", "TIMEOUT", "AUTH_ERROR", "NETWORK_ERROR", null };

        foreach (var partner in partners)
        {
            // Generate events for the last 30 days
            for (int day = 0; day < 30; day++)
            {
                var eventDate = DateTime.UtcNow.Date.AddDays(-day);
                var eventsPerDay = _random.Next(5, 25); // 5-25 events per day

                for (int i = 0; i < eventsPerDay; i++)
                {
                    var receivedAt = eventDate.AddHours(_random.Next(0, 24)).AddMinutes(_random.Next(0, 60));
                    var status = statuses[_random.Next(statuses.Length)];
                    var isSuccess = status == FileStatus.Success;
                    var isFailed = status == FileStatus.Failed;

                    var fileEvent = new FileTransferEvent
                    {
                        FileId = Guid.NewGuid(),
                        PartnerId = partner.PartnerId,
                        Direction = _random.NextDouble() > 0.6 ? FileDirection.Inbound : FileDirection.Outbound,
                        DocType = docTypes[_random.Next(docTypes.Length)],
                        SizeBytes = _random.Next(1024, 10 * 1024 * 1024), // 1KB to 10MB
                        ReceivedAt = receivedAt,
                        ProcessedAt = isSuccess || isFailed ? receivedAt.AddMinutes(_random.Next(1, 30)) : null,
                        Status = status,
                        CorrelationId = Guid.NewGuid().ToString("N")[..16],
                        ErrorCode = isFailed ? errorCodes[_random.Next(errorCodes.Length - 1)] : null,
                        ErrorMessage = isFailed ? GenerateRandomErrorMessage() : null,
                        RetryCount = isFailed ? _random.Next(0, 3) : 0
                    };

                    // ProcessingLatencyMs is computed automatically from ProcessedAt and ReceivedAt

                    fileEvents.Add(fileEvent);
                }
            }
        }

        return fileEvents;
    }

    private List<SftpConnectionEvent> CreateSampleConnectionEvents(List<Partner> partners)
    {
        var connectionEvents = new List<SftpConnectionEvent>();
        var outcomes = new[] { ConnectionOutcome.Success, ConnectionOutcome.Failed, ConnectionOutcome.AuthFailed };

        foreach (var partner in partners)
        {
            // Generate connection events for the last 7 days (more frequent than file events)
            for (int day = 0; day < 7; day++)
            {
                var eventDate = DateTime.UtcNow.Date.AddDays(-day);
                var connectionsPerDay = _random.Next(20, 100); // 20-100 connections per day

                for (int i = 0; i < connectionsPerDay; i++)
                {
                    var occurredAt = eventDate.AddHours(_random.Next(0, 24)).AddMinutes(_random.Next(0, 60)).AddSeconds(_random.Next(0, 60));
                    var outcome = outcomes[_random.Next(outcomes.Length)];

                    // Bias towards success (80% success rate)
                    if (_random.NextDouble() < 0.8)
                    {
                        outcome = ConnectionOutcome.Success;
                    }

                    connectionEvents.Add(new SftpConnectionEvent
                    {
                        EventId = Guid.NewGuid(),
                        PartnerId = partner.PartnerId,
                        OccurredAt = occurredAt,
                        Outcome = outcome,
                        ConnectionTimeMs = outcome == ConnectionOutcome.Success ? _random.Next(50, 2000) : _random.Next(5000, 30000)
                    });
                }
            }
        }

        return connectionEvents;
    }

    private List<AuditEvent> CreateSampleAuditEvents(List<Partner> partners)
    {
        var auditEvents = new List<AuditEvent>();
        var operationTypes = new[] {
            AuditOperationType.KeyUpload,
            AuditOperationType.KeyGenerate,
            AuditOperationType.KeyRevoke,
            AuditOperationType.SftpPasswordChange,
            AuditOperationType.KeyPromote
        };
        var roles = new[] { "PartnerAdmin", "PartnerUser", "InternalSupport" };

        foreach (var partner in partners)
        {
            // Generate audit events for the last 60 days
            var auditCount = _random.Next(10, 30);

            for (int i = 0; i < auditCount; i++)
            {
                var timestamp = DateTime.UtcNow.AddDays(-_random.Next(0, 60)).AddHours(-_random.Next(0, 24));
                var operationType = operationTypes[_random.Next(operationTypes.Length)];
                var role = roles[_random.Next(roles.Length)];
                var success = _random.NextDouble() > 0.1; // 90% success rate

                auditEvents.Add(new AuditEvent
                {
                    AuditId = Guid.NewGuid(),
                    PartnerId = partner.PartnerId,
                    ActorUserId = $"user_{_random.Next(1000, 9999)}@{partner.Name.Replace(" ", "").ToLower()}.com",
                    ActorRole = role,
                    OperationType = operationType,
                    Timestamp = timestamp,
                    Success = success,
                    MetadataJson = GenerateAuditMetadata(operationType)
                });
            }
        }

        return auditEvents;
    }

    private string GenerateMockPgpKey(string identifier)
    {
        var mockKeyData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"MOCK_PGP_KEY_DATA_{identifier}_{DateTime.UtcNow.Ticks}"));
        return $@"-----BEGIN PGP PUBLIC KEY BLOCK-----

{mockKeyData}
-----END PGP PUBLIC KEY BLOCK-----";
    }

    private string GenerateMockFingerprint()
    {
        var bytes = new byte[20]; // SHA-1 length
        _random.NextBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", "");
    }

    private string GenerateRandomErrorMessage()
    {
        var messages = new[]
        {
            "Invalid document structure in line 45",
            "Missing required segment ISA",
            "Authentication timeout after 30 seconds",
            "File size exceeds maximum limit of 50MB",
            "Unsupported document type specified",
            "Network connection interrupted during transfer",
            "Encryption key validation failed",
            "Trading partner not found in directory",
            "Duplicate transaction control number detected",
            "Schema validation failed for segment GS"
        };

        return messages[_random.Next(messages.Length)];
    }

    private string GenerateAuditMetadata(AuditOperationType operationType)
    {
        return operationType switch
        {
            AuditOperationType.KeyUpload => $"{{\"keyId\":\"{Guid.NewGuid()}\",\"algorithm\":\"RSA\",\"keySize\":4096}}",
            AuditOperationType.KeyGenerate => $"{{\"keyId\":\"{Guid.NewGuid()}\",\"algorithm\":\"RSA\",\"keySize\":4096}}",
            AuditOperationType.KeyRevoke => $"{{\"keyId\":\"{Guid.NewGuid()}\",\"reason\":\"Key rotation\"}}",
            AuditOperationType.KeyPromote => $"{{\"keyId\":\"{Guid.NewGuid()}\",\"previousPrimaryKeyId\":\"{Guid.NewGuid()}\"}}",
            AuditOperationType.SftpPasswordChange => $"{{\"method\":\"auto\",\"strength\":\"strong\"}}",
            _ => "{}"
        };
    }
}