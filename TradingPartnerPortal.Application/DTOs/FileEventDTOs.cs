namespace TradingPartnerPortal.Application.DTOs;

public class FileEventListItemDto
{
    public string FileId { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string DocType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ReceivedAt { get; set; } = string.Empty;
    public string? ProcessedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

public class FileEventDetailDto : FileEventListItemDto
{
    public string PartnerId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public double? ProcessingLatencyMs { get; set; }
}