using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Infrastructure.Data;

namespace TradingPartnerPortal.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly TradingPartnerPortalDbContext _context;

    public AuditRepository(TradingPartnerPortalDbContext context)
    {
        _context = context;
    }

    public IQueryable<AuditEvent> Query()
    {
        return _context.AuditEvents;
    }

    public async Task AddAsync(AuditEvent auditEvent)
    {
        await _context.AuditEvents.AddAsync(auditEvent);
        await _context.SaveChangesAsync();
    }
}