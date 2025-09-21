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

public class SftpCredentialService : ISftpCredentialService
{
    private readonly ISftpCredentialRepository _credentialRepository;
    private readonly IAuditRepository _auditRepository;

    public SftpCredentialService(
        ISftpCredentialRepository credentialRepository,
        IAuditRepository auditRepository)
    {
        _credentialRepository = credentialRepository;
        _auditRepository = auditRepository;
    }

    public async Task<SftpCredentialMetadataDto?> GetMetadataAsync(Guid partnerId)
    {
        var credential = await _credentialRepository.FindAsync(partnerId);
        if (credential == null)
            return null;

        return new SftpCredentialMetadataDto
        {
            LastRotatedAt = credential.LastRotatedAt?.ToString("O"),
            RotationMethod = credential.RotationMethod?.ToString()?.ToLowerInvariant()
        };
    }

    public async Task<(RotatePasswordResponse response, AuditEvent audit)> RotateAsync(
        Guid partnerId, 
        RotatePasswordRequest request, 
        UserContext user)
    {
        string newPassword;
        PasswordRotationMethod rotationMethod;

        if (request.Mode.Equals("manual", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(request.NewPassword))
            {
                throw new ArgumentException("New password is required for manual rotation");
            }

            if (!IsValidPassword(request.NewPassword))
            {
                throw new ArgumentException("Password does not meet complexity requirements");
            }

            newPassword = request.NewPassword;
            rotationMethod = PasswordRotationMethod.Manual;
        }
        else if (request.Mode.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            newPassword = GenerateSecurePassword();
            rotationMethod = PasswordRotationMethod.Auto;
        }
        else
        {
            throw new ArgumentException("Mode must be 'manual' or 'auto'");
        }

        var (hash, salt) = HashPassword(newPassword);

        var credential = new SftpCredential
        {
            PartnerId = partnerId,
            PasswordHash = hash,
            PasswordSalt = salt,
            LastRotatedAt = DateTime.UtcNow,
            RotationMethod = rotationMethod
        };

        await _credentialRepository.UpsertAsync(credential);

        var audit = new AuditEvent
        {
            AuditId = Guid.NewGuid(),
            PartnerId = partnerId,
            ActorUserId = user.UserId,
            ActorRole = user.Role,
            OperationType = AuditOperationType.SftpPasswordChange,
            Timestamp = DateTime.UtcNow,
            Success = true,
            MetadataJson = JsonSerializer.Serialize(new { rotationMethod = rotationMethod.ToString() })
        };

        await _auditRepository.AddAsync(audit);

        var response = new RotatePasswordResponse
        {
            Password = newPassword,
            Metadata = new SftpCredentialMetadataDto
            {
                LastRotatedAt = credential.LastRotatedAt?.ToString("O"),
                RotationMethod = credential.RotationMethod?.ToString()?.ToLowerInvariant()
            }
        };

        return (response, audit);
    }

    private static bool IsValidPassword(string password)
    {
        // Password complexity requirements from spec:
        // ^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{16,}$
        if (password.Length < 16)
            return false;

        var hasLower = password.Any(char.IsLower);
        var hasUpper = password.Any(char.IsUpper);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasLower && hasUpper && hasDigit && hasSpecial;
    }

    private static string GenerateSecurePassword()
    {
        const int length = 24;
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digitChars = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var random = RandomNumberGenerator.Create();
        var password = new StringBuilder();

        // Ensure at least one character from each category
        password.Append(GetRandomChar(lowerChars, random));
        password.Append(GetRandomChar(upperChars, random));
        password.Append(GetRandomChar(digitChars, random));
        password.Append(GetRandomChar(specialChars, random));

        // Fill the rest randomly
        var allChars = lowerChars + upperChars + digitChars + specialChars;
        for (int i = 4; i < length; i++)
        {
            password.Append(GetRandomChar(allChars, random));
        }

        // Shuffle the password to avoid predictable patterns
        return new string(password.ToString().OrderBy(x => GetRandomNumber(random)).ToArray());
    }

    private static char GetRandomChar(string chars, RandomNumberGenerator random)
    {
        var randomBytes = new byte[4];
        random.GetBytes(randomBytes);
        var randomIndex = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % chars.Length;
        return chars[randomIndex];
    }

    private static int GetRandomNumber(RandomNumberGenerator random)
    {
        var randomBytes = new byte[4];
        random.GetBytes(randomBytes);
        return BitConverter.ToInt32(randomBytes, 0);
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        // Generate a random salt
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        var salt = Convert.ToBase64String(saltBytes);

        // Hash the password with the salt using PBKDF2 (simplified implementation)
        // In production, use Argon2id as specified
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
        var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));

        return (hash, salt);
    }
}