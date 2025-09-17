using Microsoft.EntityFrameworkCore;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Infrastructure.Data;

namespace TradingPartnerPortal.Infrastructure.Repositories;

public class KeyRepository : IKeyRepository
{
    private readonly TradingPartnerPortalDbContext _context;

    public KeyRepository(TradingPartnerPortalDbContext context)
    {
        _context = context;
    }

    public IQueryable<PgpKey> Query(Guid partnerId)
    {
        return _context.PgpKeys.Where(k => k.PartnerId == partnerId);
    }

    public async Task<PgpKey?> FindAsync(Guid keyId)
    {
        return await _context.PgpKeys.FindAsync(keyId);
    }

    public async Task AddAsync(PgpKey key)
    {
        await _context.PgpKeys.AddAsync(key);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PgpKey key)
    {
        _context.PgpKeys.Update(key);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(PgpKey key)
    {
        _context.PgpKeys.Remove(key);
        await _context.SaveChangesAsync();
    }
}