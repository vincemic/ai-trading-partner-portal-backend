using TradingPartnerPortal.Application.DTOs;

namespace TradingPartnerPortal.Application.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid partnerId);
    Task<TimeSeriesResponse> GetTimeSeriesAsync(Guid partnerId, DateTime from, DateTime to);
    Task<TopErrorsResponse> GetTopErrorsAsync(Guid partnerId, DateTime from, DateTime to, int top);
}