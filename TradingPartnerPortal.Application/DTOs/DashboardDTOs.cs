namespace TradingPartnerPortal.Application.DTOs;

public class DashboardSummaryDto
{
    public int InboundFiles24h { get; set; }
    public int OutboundFiles24h { get; set; }
    public double SuccessRate { get; set; }
    public double AvgProcessingTime { get; set; }
    public int OpenErrors { get; set; }
    public long TotalBytes24h { get; set; }
    public double AvgFileSizeBytes { get; set; }
    public int LargeFileCount24h { get; set; }
}

public class TimeSeriesPointDto
{
    public string Timestamp { get; set; } = string.Empty;
    public int InboundCount { get; set; }
    public int OutboundCount { get; set; }
}

public class TimeSeriesResponse
{
    public List<TimeSeriesPointDto> Points { get; set; } = new();
}

public class ErrorCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopErrorsResponse
{
    public List<ErrorCategoryDto> Categories { get; set; } = new();
}

public class ConnectionHealthPointDto
{
    public string Timestamp { get; set; } = string.Empty;
    public int Success { get; set; }
    public int Failed { get; set; }
    public int AuthFailed { get; set; }
    public double SuccessRatePct { get; set; }
}

public class ConnectionCurrentStatusDto
{
    public string PartnerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LastCheck { get; set; } = string.Empty;
}

public class ThroughputPointDto
{
    public string Timestamp { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public int FileCount { get; set; }
    public double AvgFileSizeBytes { get; set; }
}

public class LargeFileDto
{
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ReceivedAt { get; set; } = string.Empty;
}

public class ConnectionPerformancePointDto
{
    public string Timestamp { get; set; } = string.Empty;
    public double AvgMs { get; set; }
    public double P95Ms { get; set; }
    public double MaxMs { get; set; }
    public int Count { get; set; }
}

public class DailyOpsPointDto
{
    public string Date { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int SuccessfulFiles { get; set; }
    public int FailedFiles { get; set; }
    public double SuccessRatePct { get; set; }
}

public class FailureBurstPointDto
{
    public string WindowStart { get; set; } = string.Empty;
    public int FailureCount { get; set; }
}

public class ZeroFileWindowStatusDto
{
    public int WindowHours { get; set; }
    public int InboundFiles { get; set; }
    public bool Flagged { get; set; }
}