using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;

namespace TradingPartnerPortal.Application.Services;

public interface IAdvancedMetricsService
{
    Task<IReadOnlyList<ConnectionHealthPointDto>> GetConnectionHealthAsync(Guid partnerId, DateTime from, DateTime to);
    Task<ConnectionCurrentStatusDto> GetConnectionStatusAsync(Guid partnerId);
    Task<IReadOnlyList<ThroughputPointDto>> GetThroughputAsync(Guid partnerId, DateTime from, DateTime to);
    Task<IReadOnlyList<LargeFileDto>> GetLargeFilesAsync(Guid partnerId, LargeFileQuery query);
    Task<IReadOnlyList<ConnectionPerformancePointDto>> GetConnectionPerformanceAsync(Guid partnerId, DateTime from, DateTime to);
    Task<IReadOnlyList<DailyOpsPointDto>> GetDailySummaryAsync(Guid partnerId, DailySummaryQuery query);
    Task<IReadOnlyList<FailureBurstPointDto>> GetFailureBurstsAsync(Guid partnerId, FailureBurstQuery query);
    Task<ZeroFileWindowStatusDto> GetZeroFileWindowStatusAsync(Guid partnerId, ZeroFileWindowQuery query);
}