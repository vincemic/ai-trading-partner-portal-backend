using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Domain.Entities;

public class Partner
{
    public Guid PartnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PartnerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<PgpKey> PgpKeys { get; set; } = new List<PgpKey>();
    public virtual SftpCredential? SftpCredential { get; set; }
    public virtual ICollection<FileTransferEvent> FileTransferEvents { get; set; } = new List<FileTransferEvent>();
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
}