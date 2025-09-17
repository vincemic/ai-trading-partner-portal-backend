using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Repositories;

public interface IAuditRepository
{
    IQueryable<AuditEvent> Query();
    Task AddAsync(AuditEvent auditEvent);
}