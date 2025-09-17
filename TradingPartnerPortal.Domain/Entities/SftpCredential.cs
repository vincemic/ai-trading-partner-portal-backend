using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Domain.Entities;

public class SftpCredential
{
    public Guid PartnerId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public DateTime? LastRotatedAt { get; set; }
    public PasswordRotationMethod? RotationMethod { get; set; }
    
    // Navigation properties
    public virtual Partner Partner { get; set; } = null!;
}