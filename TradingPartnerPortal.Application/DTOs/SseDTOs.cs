namespace TradingPartnerPortal.Application.DTOs;

// SSE Event Data Types
public class FileCreatedEventData
{
    public string FileId { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string DocType { get; set; } = string.Empty;
}

public class FileStatusChangedEventData
{
    public string FileId { get; set; } = string.Empty;
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
}

public class KeyPromotedEventData
{
    public string KeyId { get; set; } = string.Empty;
    public string? PreviousPrimaryKeyId { get; set; }
}

public class KeyRevokedEventData
{
    public string KeyId { get; set; } = string.Empty;
}

public class DashboardMetricsTickData
{
    public DashboardSummaryDto Summary { get; set; } = new();
}