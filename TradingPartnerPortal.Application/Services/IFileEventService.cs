using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;

namespace TradingPartnerPortal.Application.Services;

public interface IFileEventService
{
    Task<Paged<FileEventListItemDto>> SearchAsync(Guid partnerId, FileSearchCriteria criteria);
    Task<FileEventDetailDto?> GetAsync(Guid partnerId, Guid fileId);
}