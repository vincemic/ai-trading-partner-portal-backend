namespace TradingPartnerPortal.Domain.Entities;

public class SseEventCursor
{
    public Guid PartnerId { get; set; }
    public long LastSequence { get; set; }
    
    // Navigation properties
    public virtual Partner Partner { get; set; } = null!;
}