using Microsoft.EntityFrameworkCore;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IFileEventRepository _fileEventRepository;
    private readonly IConnectionEventRepository _connectionEventRepository;

    public DashboardService(
        IFileEventRepository fileEventRepository,
        IConnectionEventRepository connectionEventRepository)
    {
        _fileEventRepository = fileEventRepository;
        _connectionEventRepository = connectionEventRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid partnerId)
    {
        var now = DateTime.UtcNow;
        var yesterday = now.AddHours(-24);

        var files24h = await _fileEventRepository.Query(partnerId)
            .Where(f => f.ReceivedAt >= yesterday)
            .ToListAsync();

        var inboundFiles = files24h.Count(f => f.Direction == FileDirection.Inbound);
        var outboundFiles = files24h.Count(f => f.Direction == FileDirection.Outbound);
        var successfulFiles = files24h.Count(f => f.Status == FileStatus.Success);
        var totalFiles = files24h.Count;
        var openErrors = files24h.Count(f => f.Status == FileStatus.Failed);

        var successRate = totalFiles > 0 ? (double)successfulFiles / totalFiles * 100 : 0;

        var processedFiles = files24h.Where(f => f.ProcessedAt.HasValue).ToList();
        var avgProcessingTime = processedFiles.Any() 
            ? processedFiles.Average(f => f.ProcessingLatencyMs ?? 0) 
            : 0;

        var totalBytes = files24h.Sum(f => f.SizeBytes);
        var avgFileSize = files24h.Any() ? files24h.Average(f => f.SizeBytes) : 0;
        var largeFileCount = files24h.Count(f => f.SizeBytes > 10 * 1024 * 1024); // >10MB

        return new DashboardSummaryDto
        {
            InboundFiles24h = inboundFiles,
            OutboundFiles24h = outboundFiles,
            SuccessRate = Math.Round(successRate, 2),
            AvgProcessingTime = Math.Round(avgProcessingTime, 2),
            OpenErrors = openErrors,
            TotalBytes24h = totalBytes,
            AvgFileSizeBytes = Math.Round(avgFileSize, 2),
            LargeFileCount24h = largeFileCount
        };
    }

    public async Task<TimeSeriesResponse> GetTimeSeriesAsync(Guid partnerId, DateTime from, DateTime to)
    {
        var files = await _fileEventRepository.Query(partnerId)
            .Where(f => f.ReceivedAt >= from && f.ReceivedAt <= to)
            .ToListAsync();

        var hourlyGroups = files
            .GroupBy(f => new DateTime(f.ReceivedAt.Year, f.ReceivedAt.Month, f.ReceivedAt.Day, f.ReceivedAt.Hour, 0, 0))
            .Select(g => new TimeSeriesPointDto
            {
                Timestamp = g.Key.ToString("O"),
                InboundCount = g.Count(f => f.Direction == FileDirection.Inbound),
                OutboundCount = g.Count(f => f.Direction == FileDirection.Outbound)
            })
            .OrderBy(p => p.Timestamp)
            .ToList();

        return new TimeSeriesResponse { Points = hourlyGroups };
    }

    public async Task<TopErrorsResponse> GetTopErrorsAsync(Guid partnerId, DateTime from, DateTime to, int top)
    {
        var errors = await _fileEventRepository.Query(partnerId)
            .Where(f => f.Status == FileStatus.Failed && f.ReceivedAt >= from && f.ReceivedAt <= to)
            .Where(f => !string.IsNullOrEmpty(f.ErrorCode))
            .ToListAsync();

        var errorCategories = errors
            .GroupBy(f => f.ErrorCode!)
            .Select(g => new ErrorCategoryDto
            {
                Category = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(e => e.Count)
            .Take(top)
            .ToList();

        return new TopErrorsResponse { Categories = errorCategories };
    }
}