namespace TradingPartnerPortal.Application.DTOs;

public class SftpCredentialMetadataDto
{
    public string? LastRotatedAt { get; set; }
    public string? RotationMethod { get; set; }
}

public class RotatePasswordRequest
{
    public string Mode { get; set; } = string.Empty; // "manual" or "auto"
    public string? NewPassword { get; set; }
}

public class RotatePasswordResponse
{
    public string? Password { get; set; }
    public SftpCredentialMetadataDto Metadata { get; set; } = new();
}