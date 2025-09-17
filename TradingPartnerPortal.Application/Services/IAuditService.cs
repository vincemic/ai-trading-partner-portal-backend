using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;

namespace TradingPartnerPortal.Application.Services;

public interface IAuditService
{
    Task<Paged<AuditEventDto>> SearchAsync(AuditSearchCriteria criteria);
}