using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Domain.Entities;

public class PgpKey
{
    public Guid KeyId { get; set; }
    public Guid PartnerId { get; set; }
    public string PublicKeyArmored { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime? RevokedAt { get; set; }
    public PgpKeyStatus Status { get; set; }
    public bool IsPrimary { get; set; }
    
    // Navigation properties
    public virtual Partner Partner { get; set; } = null!;
}