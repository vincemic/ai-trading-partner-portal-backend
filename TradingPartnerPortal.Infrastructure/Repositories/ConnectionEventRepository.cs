using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Infrastructure.Data;

namespace TradingPartnerPortal.Infrastructure.Repositories;

public class ConnectionEventRepository : IConnectionEventRepository
{
    private readonly TradingPartnerPortalDbContext _context;

    public ConnectionEventRepository(TradingPartnerPortalDbContext context)
    {
        _context = context;
    }

    public IQueryable<SftpConnectionEvent> Query(Guid partnerId)
    {
        return _context.SftpConnectionEvents.Where(c => c.PartnerId == partnerId);
    }

    public async Task AddAsync(SftpConnectionEvent connectionEvent)
    {
        await _context.SftpConnectionEvents.AddAsync(connectionEvent);
        await _context.SaveChangesAsync();
    }
}