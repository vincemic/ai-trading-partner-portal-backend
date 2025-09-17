using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Domain.Entities;

public class FileTransferEvent
{
    public Guid FileId { get; set; }
    public Guid PartnerId { get; set; }
    public FileDirection Direction { get; set; }
    public string DocType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public FileStatus Status { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    
    // Computed property
    public double? ProcessingLatencyMs => ProcessedAt.HasValue 
        ? (ProcessedAt.Value - ReceivedAt).TotalMilliseconds 
        : null;
    
    // Navigation properties
    public virtual Partner Partner { get; set; } = null!;
}