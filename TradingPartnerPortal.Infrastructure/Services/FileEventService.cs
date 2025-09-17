using Microsoft.EntityFrameworkCore;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Application.Services;

namespace TradingPartnerPortal.Infrastructure.Services;

public class FileEventService : IFileEventService
{
    private readonly IFileEventRepository _fileEventRepository;

    public FileEventService(IFileEventRepository fileEventRepository)
    {
        _fileEventRepository = fileEventRepository;
    }

    public async Task<Paged<FileEventListItemDto>> SearchAsync(Guid partnerId, FileSearchCriteria criteria)
    {
        var query = _fileEventRepository.Query(partnerId);

        // Apply filters
        if (criteria.Direction.HasValue)
        {
            query = query.Where(f => f.Direction == criteria.Direction.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(f => f.Status == criteria.Status.Value);
        }

        if (!string.IsNullOrEmpty(criteria.DocType))
        {
            query = query.Where(f => f.DocType == criteria.DocType);
        }

        if (criteria.DateFrom.HasValue)
        {
            query = query.Where(f => f.ReceivedAt >= criteria.DateFrom.Value);
        }

        if (criteria.DateTo.HasValue)
        {
            query = query.Where(f => f.ReceivedAt <= criteria.DateTo.Value);
        }

        // Get total count for pagination
        var totalItems = await query.CountAsync();

        // Apply pagination and ordering
        var files = await query
            .OrderByDescending(f => f.ReceivedAt)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync();

        var items = files.Select(f => new FileEventListItemDto
        {
            FileId = f.FileId.ToString(),
            Direction = f.Direction.ToString(),
            DocType = f.DocType,
            SizeBytes = f.SizeBytes,
            ReceivedAt = f.ReceivedAt.ToString("O"),
            ProcessedAt = f.ProcessedAt?.ToString("O"),
            Status = f.Status.ToString(),
            ErrorCode = f.ErrorCode
        }).ToList();

        return new Paged<FileEventListItemDto>
        {
            Items = items,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling((double)totalItems / criteria.PageSize)
        };
    }

    public async Task<FileEventDetailDto?> GetAsync(Guid partnerId, Guid fileId)
    {
        var fileEvent = await _fileEventRepository.FindAsync(partnerId, fileId);
        if (fileEvent == null)
        {
            return null;
        }

        return new FileEventDetailDto
        {
            FileId = fileEvent.FileId.ToString(),
            PartnerId = fileEvent.PartnerId.ToString(),
            Direction = fileEvent.Direction.ToString(),
            DocType = fileEvent.DocType,
            SizeBytes = fileEvent.SizeBytes,
            ReceivedAt = fileEvent.ReceivedAt.ToString("O"),
            ProcessedAt = fileEvent.ProcessedAt?.ToString("O"),
            Status = fileEvent.Status.ToString(),
            CorrelationId = fileEvent.CorrelationId,
            ErrorCode = fileEvent.ErrorCode,
            ErrorMessage = fileEvent.ErrorMessage,
            RetryCount = fileEvent.RetryCount,
            ProcessingLatencyMs = fileEvent.ProcessingLatencyMs
        };
    }
}