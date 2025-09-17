using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Infrastructure.Services;

public class KeyService : IKeyService
{
    private readonly IKeyRepository _keyRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ISseEventService _sseEventService;

    public KeyService(
        IKeyRepository keyRepository,
        IAuditRepository auditRepository,
        ISseEventService sseEventService)
    {
        _keyRepository = keyRepository;
        _auditRepository = auditRepository;
        _sseEventService = sseEventService;
    }

    public async Task<IReadOnlyList<KeySummaryDto>> ListAsync(Guid partnerId)
    {
        var keys = await _keyRepository.Query(partnerId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return keys.Select(MapToSummaryDto).ToList();
    }

    public async Task<(KeySummaryDto key, AuditEvent audit)> UploadAsync(
        Guid partnerId, 
        UploadKeyRequest request, 
        UserContext user)
    {
        // Validate PGP key format
        if (!IsValidPgpKey(request.PublicKeyArmored))
        {
            throw new ArgumentException("Invalid PGP key format");
        }

        var fingerprint = ExtractFingerprint(request.PublicKeyArmored);
        var algorithm = ExtractAlgorithm(request.PublicKeyArmored);
        var keySize = ExtractKeySize(request.PublicKeyArmored);

        // Check for duplicate fingerprint
        var existingKey = await _keyRepository.Query(partnerId)
            .FirstOrDefaultAsync(k => k.Fingerprint == fingerprint);
        
        if (existingKey != null)
        {
            throw new InvalidOperationException("Key with this fingerprint already exists");
        }

        var validFrom = ParseDateTime(request.ValidFrom) ?? DateTime.UtcNow;
        var validTo = ParseDateTime(request.ValidTo);

        var key = new PgpKey
        {
            KeyId = Guid.NewGuid(),
            PartnerId = partnerId,
            PublicKeyArmored = request.PublicKeyArmored,
            Fingerprint = fingerprint,
            Algorithm = algorithm,
            KeySize = keySize,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Status = validFrom > DateTime.UtcNow ? PgpKeyStatus.PendingActivation : PgpKeyStatus.Active,
            IsPrimary = false
        };

        // Handle primary key promotion
        if (request.MakePrimary == true)
        {
            await DemoteCurrentPrimaryKey(partnerId);
            key.IsPrimary = true;
        }

        await _keyRepository.AddAsync(key);

        var audit = new AuditEvent
        {
            AuditId = Guid.NewGuid(),
            PartnerId = partnerId,
            ActorUserId = user.UserId,
            ActorRole = user.Role,
            OperationType = AuditOperationType.KeyUpload,
            Timestamp = DateTime.UtcNow,
            Success = true,
            MetadataJson = JsonSerializer.Serialize(new { keyId = key.KeyId, fingerprint })
        };

        await _auditRepository.AddAsync(audit);

        if (key.IsPrimary)
        {
            _sseEventService.PublishPartnerEvent(partnerId, "key.promoted", new KeyPromotedEventData
            {
                KeyId = key.KeyId.ToString(),
                PreviousPrimaryKeyId = null // TODO: Track previous primary
            });
        }

        return (MapToSummaryDto(key), audit);
    }

    public async Task<(GenerateKeyResponse response, AuditEvent audit)> GenerateAsync(
        Guid partnerId, 
        GenerateKeyRequest request, 
        UserContext user)
    {
        var validFrom = ParseDateTime(request.ValidFrom) ?? DateTime.UtcNow;
        var validTo = ParseDateTime(request.ValidTo);

        // Generate RSA 4096 key pair
        var (privateKeyArmored, publicKeyArmored) = GenerateRsaKeyPair();
        
        var fingerprint = ExtractFingerprint(publicKeyArmored);

        var key = new PgpKey
        {
            KeyId = Guid.NewGuid(),
            PartnerId = partnerId,
            PublicKeyArmored = publicKeyArmored,
            Fingerprint = fingerprint,
            Algorithm = "RSA",
            KeySize = 4096,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Status = validFrom > DateTime.UtcNow ? PgpKeyStatus.PendingActivation : PgpKeyStatus.Active,
            IsPrimary = false
        };

        // Handle primary key promotion
        if (request.MakePrimary == true)
        {
            await DemoteCurrentPrimaryKey(partnerId);
            key.IsPrimary = true;
        }

        await _keyRepository.AddAsync(key);

        var audit = new AuditEvent
        {
            AuditId = Guid.NewGuid(),
            PartnerId = partnerId,
            ActorUserId = user.UserId,
            ActorRole = user.Role,
            OperationType = AuditOperationType.KeyGenerate,
            Timestamp = DateTime.UtcNow,
            Success = true,
            MetadataJson = JsonSerializer.Serialize(new { keyId = key.KeyId, fingerprint })
        };

        await _auditRepository.AddAsync(audit);

        if (key.IsPrimary)
        {
            _sseEventService.PublishPartnerEvent(partnerId, "key.promoted", new KeyPromotedEventData
            {
                KeyId = key.KeyId.ToString(),
                PreviousPrimaryKeyId = null
            });
        }

        var response = new GenerateKeyResponse
        {
            PrivateKeyArmored = privateKeyArmored,
            Key = MapToSummaryDto(key)
        };

        return (response, audit);
    }

    public async Task<AuditEvent> RevokeAsync(
        Guid partnerId, 
        Guid keyId, 
        RevokeKeyRequest request, 
        UserContext user)
    {
        var key = await _keyRepository.FindAsync(keyId);
        if (key == null || key.PartnerId != partnerId)
        {
            throw new ArgumentException("Key not found");
        }

        if (key.Status == PgpKeyStatus.Revoked)
        {
            throw new InvalidOperationException("Key is already revoked");
        }

        key.Status = PgpKeyStatus.Revoked;
        key.RevokedAt = DateTime.UtcNow;

        // If this was the primary key, promote another one
        if (key.IsPrimary)
        {
            key.IsPrimary = false;
            await PromoteNextAvailableKey(partnerId);
        }

        await _keyRepository.UpdateAsync(key);

        var audit = new AuditEvent
        {
            AuditId = Guid.NewGuid(),
            PartnerId = partnerId,
            ActorUserId = user.UserId,
            ActorRole = user.Role,
            OperationType = AuditOperationType.KeyRevoke,
            Timestamp = DateTime.UtcNow,
            Success = true,
            MetadataJson = JsonSerializer.Serialize(new { keyId, reason = request.Reason })
        };

        await _auditRepository.AddAsync(audit);

        _sseEventService.PublishPartnerEvent(partnerId, "key.revoked", new KeyRevokedEventData
        {
            KeyId = keyId.ToString()
        });

        return audit;
    }

    public async Task<AuditEvent?> PromoteAsync(Guid partnerId, Guid keyId, UserContext user)
    {
        var key = await _keyRepository.FindAsync(keyId);
        if (key == null || key.PartnerId != partnerId)
        {
            throw new ArgumentException("Key not found");
        }

        if (key.Status != PgpKeyStatus.Active)
        {
            throw new InvalidOperationException("Only active keys can be promoted to primary");
        }

        if (key.IsPrimary)
        {
            return null; // Already primary
        }

        var previousPrimaryKey = await DemoteCurrentPrimaryKey(partnerId);
        key.IsPrimary = true;
        await _keyRepository.UpdateAsync(key);

        var audit = new AuditEvent
        {
            AuditId = Guid.NewGuid(),
            PartnerId = partnerId,
            ActorUserId = user.UserId,
            ActorRole = user.Role,
            OperationType = AuditOperationType.KeyPromote,
            Timestamp = DateTime.UtcNow,
            Success = true,
            MetadataJson = JsonSerializer.Serialize(new { keyId, previousPrimaryKeyId = previousPrimaryKey?.KeyId })
        };

        await _auditRepository.AddAsync(audit);

        _sseEventService.PublishPartnerEvent(partnerId, "key.promoted", new KeyPromotedEventData
        {
            KeyId = keyId.ToString(),
            PreviousPrimaryKeyId = previousPrimaryKey?.KeyId.ToString()
        });

        return audit;
    }

    private async Task<PgpKey?> DemoteCurrentPrimaryKey(Guid partnerId)
    {
        var currentPrimary = await _keyRepository.Query(partnerId)
            .FirstOrDefaultAsync(k => k.IsPrimary);

        if (currentPrimary != null)
        {
            currentPrimary.IsPrimary = false;
            await _keyRepository.UpdateAsync(currentPrimary);
        }

        return currentPrimary;
    }

    private async Task PromoteNextAvailableKey(Guid partnerId)
    {
        var nextKey = await _keyRepository.Query(partnerId)
            .Where(k => k.Status == PgpKeyStatus.Active && !k.IsPrimary)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync();

        if (nextKey != null)
        {
            nextKey.IsPrimary = true;
            await _keyRepository.UpdateAsync(nextKey);
        }
    }

    private static KeySummaryDto MapToSummaryDto(PgpKey key)
    {
        return new KeySummaryDto
        {
            KeyId = key.KeyId.ToString(),
            Fingerprint = key.Fingerprint,
            Algorithm = key.Algorithm,
            KeySize = key.KeySize,
            CreatedAt = key.CreatedAt.ToString("O"),
            ValidFrom = key.ValidFrom.ToString("O"),
            ValidTo = key.ValidTo?.ToString("O"),
            Status = key.Status.ToString(),
            IsPrimary = key.IsPrimary
        };
    }

    private static DateTime? ParseDateTime(string? dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
            return null;

        return DateTime.TryParse(dateTimeString, out var result) ? result : null;
    }

    private static bool IsValidPgpKey(string publicKeyArmored)
    {
        return publicKeyArmored.Contains("-----BEGIN PGP PUBLIC KEY BLOCK-----") &&
               publicKeyArmored.Contains("-----END PGP PUBLIC KEY BLOCK-----");
    }

    private static string ExtractFingerprint(string publicKeyArmored)
    {
        // Simple mock implementation - in real scenario, use BouncyCastle
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(publicKeyArmored));
        return BitConverter.ToString(hash).Replace("-", "");
    }

    private static string ExtractAlgorithm(string publicKeyArmored)
    {
        // Mock implementation - normally parsed from key
        return "RSA";
    }

    private static int ExtractKeySize(string publicKeyArmored)
    {
        // Mock implementation - normally parsed from key
        return 4096;
    }

    private static (string privateKey, string publicKey) GenerateRsaKeyPair()
    {
        using var rsa = RSA.Create(4096);
        
        // Export keys in PEM format (simplified)
        var privateKeyPem = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKeyPem = Convert.ToBase64String(rsa.ExportRSAPublicKey());

        var privateKeyArmored = $"-----BEGIN PGP PRIVATE KEY BLOCK-----\n{privateKeyPem}\n-----END PGP PRIVATE KEY BLOCK-----";
        var publicKeyArmored = $"-----BEGIN PGP PUBLIC KEY BLOCK-----\n{publicKeyPem}\n-----END PGP PUBLIC KEY BLOCK-----";

        return (privateKeyArmored, publicKeyArmored);
    }
}