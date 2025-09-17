using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Domain.Entities;

public class AuditEvent
{
    public Guid AuditId { get; set; }
    public Guid PartnerId { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public AuditOperationType OperationType { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string MetadataJson { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual Partner Partner { get; set; } = null!;
}