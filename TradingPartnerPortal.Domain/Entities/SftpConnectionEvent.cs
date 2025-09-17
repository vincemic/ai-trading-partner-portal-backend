using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Domain.Entities;

public class SftpConnectionEvent
{
    public Guid EventId { get; set; }
    public Guid PartnerId { get; set; }
    public DateTime OccurredAt { get; set; }
    public ConnectionOutcome Outcome { get; set; }
    public int ConnectionTimeMs { get; set; }
    
    // Navigation properties
    public virtual Partner Partner { get; set; } = null!;
}