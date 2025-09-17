using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Application.DTOs;

public class KeySummaryDto
{
    public string KeyId { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string ValidFrom { get; set; } = string.Empty;
    public string? ValidTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class UploadKeyRequest
{
    public string PublicKeyArmored { get; set; } = string.Empty;
    public string? ValidFrom { get; set; }
    public string? ValidTo { get; set; }
    public bool? MakePrimary { get; set; }
}

public class GenerateKeyRequest
{
    public string? ValidFrom { get; set; }
    public string? ValidTo { get; set; }
    public bool? MakePrimary { get; set; }
}

public class GenerateKeyResponse
{
    public string PrivateKeyArmored { get; set; } = string.Empty;
    public KeySummaryDto Key { get; set; } = new();
}

public class RevokeKeyRequest
{
    public string? Reason { get; set; }
}

public class PromoteKeyRequest
{
    // Empty for now as specified in the spec
}