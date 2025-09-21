using Microsoft.EntityFrameworkCore;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.Infrastructure.Services;

public class AdvancedMetricsService : IAdvancedMetricsService
{
    private readonly IFileEventRepository _fileEventRepository;
    private readonly IConnectionEventRepository _connectionEventRepository;

    public AdvancedMetricsService(
        IFileEventRepository fileEventRepository,
        IConnectionEventRepository connectionEventRepository)
    {
        _fileEventRepository = fileEventRepository;
        _connectionEventRepository = connectionEventRepository;
    }

    public async Task<IReadOnlyList<ConnectionHealthPointDto>> GetConnectionHealthAsync(Guid partnerId, DateTime from, DateTime to)
    {
        var connectionEvents = await _connectionEventRepository.Query(partnerId)
            .Where(c => c.OccurredAt >= from && c.OccurredAt <= to)
            .ToListAsync();

        var hourlyGroups = connectionEvents
            .GroupBy(c => new DateTime(c.OccurredAt.Year, c.OccurredAt.Month, c.OccurredAt.Day, c.OccurredAt.Hour, 0, 0))
            .Select(g => new ConnectionHealthPointDto
            {
                Timestamp = g.Key.ToString("O"),
                Success = g.Count(c => c.Outcome == ConnectionOutcome.Success),
                Failed = g.Count(c => c.Outcome == ConnectionOutcome.Failed),
                AuthFailed = g.Count(c => c.Outcome == ConnectionOutcome.AuthFailed),
                SuccessRatePct = g.Any() ? Math.Round((double)g.Count(c => c.Outcome == ConnectionOutcome.Success) / g.Count() * 100, 2) : 0
            })
            .OrderBy(p => p.Timestamp)
            .ToList();

        return hourlyGroups;
    }

    public async Task<ConnectionCurrentStatusDto> GetConnectionStatusAsync(Guid partnerId)
    {
        var latestConnection = await _connectionEventRepository.Query(partnerId)
            .OrderByDescending(c => c.OccurredAt)
            .FirstOrDefaultAsync();

        if (latestConnection == null)
        {
            return new ConnectionCurrentStatusDto
            {
                PartnerId = partnerId.ToString(),
                Status = "Unknown",
                LastCheck = DateTime.UtcNow.ToString("O")
            };
        }

        var status = latestConnection.Outcome switch
        {
            ConnectionOutcome.Success => "Connected",
            ConnectionOutcome.Failed => "Failed",
            ConnectionOutcome.AuthFailed => "Authentication Failed",
            _ => "Unknown"
        };

        return new ConnectionCurrentStatusDto
        {
            PartnerId = partnerId.ToString(),
            Status = status,
            LastCheck = latestConnection.OccurredAt.ToString("O")
        };
    }

    public async Task<IReadOnlyList<ThroughputPointDto>> GetThroughputAsync(Guid partnerId, DateTime from, DateTime to)
    {
        var files = await _fileEventRepository.Query(partnerId)
            .Where(f => f.ReceivedAt >= from && f.ReceivedAt <= to)
            .ToListAsync();

        var hourlyGroups = files
            .GroupBy(f => new DateTime(f.ReceivedAt.Year, f.ReceivedAt.Month, f.ReceivedAt.Day, f.ReceivedAt.Hour, 0, 0))
            .Select(g => new ThroughputPointDto
            {
                Timestamp = g.Key.ToString("O"),
                TotalBytes = g.Sum(f => f.SizeBytes),
                FileCount = g.Count(),
                AvgFileSizeBytes = g.Any() ? Math.Round(g.Average(f => f.SizeBytes), 2) : 0
            })
            .OrderBy(p => p.Timestamp)
            .ToList();

        return hourlyGroups;
    }

    public async Task<IReadOnlyList<LargeFileDto>> GetLargeFilesAsync(Guid partnerId, LargeFileQuery query)
    {
        var largeFilesQuery = await _fileEventRepository.Query(partnerId)
            .Where(f => f.ReceivedAt >= query.From && f.ReceivedAt <= query.To)
            .OrderByDescending(f => f.SizeBytes)
            .Take(query.Limit)
            .ToListAsync();

        var largeFiles = largeFilesQuery.Select(f => new LargeFileDto
        {
            FileName = $"file_{f.FileId.ToString().Substring(0, 8)}.{(f.DocType?.ToLower() ?? "edi")}",
            SizeBytes = f.SizeBytes,
            ReceivedAt = f.ReceivedAt.ToString("O")
        }).ToList();

        return largeFiles;
    }

    public async Task<IReadOnlyList<ConnectionPerformancePointDto>> GetConnectionPerformanceAsync(Guid partnerId, DateTime from, DateTime to)
    {
        var connectionEvents = await _connectionEventRepository.Query(partnerId)
            .Where(c => c.OccurredAt >= from && c.OccurredAt <= to && c.Outcome == ConnectionOutcome.Success)
            .ToListAsync();

        var hourlyGroups = connectionEvents
            .GroupBy(c => new DateTime(c.OccurredAt.Year, c.OccurredAt.Month, c.OccurredAt.Day, c.OccurredAt.Hour, 0, 0))
            .Select(g => new ConnectionPerformancePointDto
            {
                Timestamp = g.Key.ToString("O"),
                AvgMs = g.Any() ? Math.Round(g.Average(c => c.ConnectionTimeMs), 2) : 0,
                P95Ms = g.Any() ? Math.Round(CalculatePercentile(g.Select(c => c.ConnectionTimeMs).ToList(), 0.95), 2) : 0,
                MaxMs = g.Any() ? g.Max(c => c.ConnectionTimeMs) : 0,
                Count = g.Count()
            })
            .OrderBy(p => p.Timestamp)
            .ToList();

        return hourlyGroups;
    }

    public async Task<IReadOnlyList<DailyOpsPointDto>> GetDailySummaryAsync(Guid partnerId, DailySummaryQuery query)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-query.Days + 1);
        var endDate = DateTime.UtcNow.Date.AddDays(1);

        var files = await _fileEventRepository.Query(partnerId)
            .Where(f => f.ReceivedAt >= startDate && f.ReceivedAt < endDate)
            .ToListAsync();

        var dailyGroups = files
            .GroupBy(f => f.ReceivedAt.Date)
            .Select(g => new DailyOpsPointDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                TotalFiles = g.Count(),
                SuccessfulFiles = g.Count(f => f.Status == FileStatus.Success),
                FailedFiles = g.Count(f => f.Status == FileStatus.Failed),
                SuccessRatePct = g.Any() ? Math.Round((double)g.Count(f => f.Status == FileStatus.Success) / g.Count() * 100, 2) : 0
            })
            .OrderBy(p => p.Date)
            .ToList();

        return dailyGroups;
    }

    public async Task<IReadOnlyList<FailureBurstPointDto>> GetFailureBurstsAsync(Guid partnerId, FailureBurstQuery query)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(query.Lookback);
        var failedFiles = await _fileEventRepository.Query(partnerId)
            .Where(f => f.Status == FileStatus.Failed && f.ReceivedAt >= cutoffTime)
            .OrderBy(f => f.ReceivedAt)
            .ToListAsync();

        const int burstWindowMinutes = 15;
        const int burstThreshold = 3;

        var burstPoints = new List<FailureBurstPointDto>();
        var currentWindow = new List<Domain.Entities.FileTransferEvent>();

        foreach (var failure in failedFiles)
        {
            // Remove events outside current window
            currentWindow.RemoveAll(f => failure.ReceivedAt.Subtract(f.ReceivedAt).TotalMinutes > burstWindowMinutes);
            
            currentWindow.Add(failure);

            // Check if we have a burst
            if (currentWindow.Count >= burstThreshold)
            {
                var windowStart = currentWindow.First().ReceivedAt;
                
                // Avoid duplicate burst points for the same window
                if (!burstPoints.Any(b => Math.Abs((DateTime.Parse(b.WindowStart) - windowStart).TotalMinutes) < burstWindowMinutes))
                {
                    burstPoints.Add(new FailureBurstPointDto
                    {
                        WindowStart = windowStart.ToString("O"),
                        FailureCount = currentWindow.Count
                    });
                }
            }
        }

        return burstPoints.OrderBy(b => b.WindowStart).ToList();
    }

    public async Task<ZeroFileWindowStatusDto> GetZeroFileWindowStatusAsync(Guid partnerId, ZeroFileWindowQuery query)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(query.Window);
        var recentInboundFiles = await _fileEventRepository.Query(partnerId)
            .Where(f => f.Direction == FileDirection.Inbound && f.ReceivedAt >= cutoffTime)
            .CountAsync();

        var windowHours = (int)query.Window.TotalHours;
        var flagged = recentInboundFiles == 0 && windowHours >= 2; // Flag if no files for 2+ hours

        return new ZeroFileWindowStatusDto
        {
            WindowHours = windowHours,
            InboundFiles = recentInboundFiles,
            Flagged = flagged
        };
    }

    private static double CalculatePercentile(List<int> values, double percentile)
    {
        if (!values.Any()) return 0;

        values.Sort();
        var index = percentile * (values.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper)
            return values[lower];

        var weight = index - lower;
        return values[lower] * (1 - weight) + values[upper] * weight;
    }
}