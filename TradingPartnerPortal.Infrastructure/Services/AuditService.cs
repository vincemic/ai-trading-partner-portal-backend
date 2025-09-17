using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Application.Services;

namespace TradingPartnerPortal.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;

    public AuditService(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<Paged<AuditEventDto>> SearchAsync(AuditSearchCriteria criteria)
    {
        var query = _auditRepository.Query();

        // Apply filters
        if (criteria.PartnerId.HasValue)
        {
            query = query.Where(a => a.PartnerId == criteria.PartnerId.Value);
        }

        if (criteria.OperationType.HasValue)
        {
            query = query.Where(a => a.OperationType == criteria.OperationType.Value);
        }

        if (criteria.DateFrom.HasValue)
        {
            query = query.Where(a => a.Timestamp >= criteria.DateFrom.Value);
        }

        if (criteria.DateTo.HasValue)
        {
            query = query.Where(a => a.Timestamp <= criteria.DateTo.Value);
        }

        // Get total count for pagination
        var totalItems = await query.CountAsync();

        // Apply pagination and ordering
        var auditEvents = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync();

        var items = auditEvents.Select(a => new AuditEventDto
        {
            AuditId = a.AuditId.ToString(),
            PartnerId = a.PartnerId.ToString(),
            ActorUserId = a.ActorUserId,
            ActorRole = a.ActorRole,
            OperationType = a.OperationType.ToString(),
            Timestamp = a.Timestamp.ToString("O"),
            Success = a.Success,
            Metadata = ParseMetadata(a.MetadataJson)
        }).ToList();

        return new Paged<AuditEventDto>
        {
            Items = items,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling((double)totalItems / criteria.PageSize)
        };
    }

    private static object? ParseMetadata(string metadataJson)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(metadataJson);
        }
        catch
        {
            return null;
        }
    }
}