using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Application.Models;

public class UserContext
{
    public string UserId { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class FileSearchCriteria
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public FileDirection? Direction { get; set; }
    public FileStatus? Status { get; set; }
    public string? DocType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class AuditSearchCriteria
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public Guid? PartnerId { get; set; }
    public AuditOperationType? OperationType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class LargeFileQuery
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Limit { get; set; } = 10;
}

public class DailySummaryQuery
{
    public int Days { get; set; } = 7;
}

public class FailureBurstQuery
{
    public TimeSpan Lookback { get; set; } = TimeSpan.FromHours(24);
}

public class ZeroFileWindowQuery
{
    public TimeSpan Window { get; set; } = TimeSpan.FromHours(4);
}