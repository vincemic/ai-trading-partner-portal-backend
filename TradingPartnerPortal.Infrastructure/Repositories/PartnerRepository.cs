using Microsoft.EntityFrameworkCore;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Infrastructure.Data;

namespace TradingPartnerPortal.Infrastructure.Repositories;

public class PartnerRepository : IPartnerRepository
{
    private readonly TradingPartnerPortalDbContext _context;

    public PartnerRepository(TradingPartnerPortalDbContext context)
    {
        _context = context;
    }

    public async Task<Partner?> FindAsync(Guid partnerId)
    {
        return await _context.Partners.FindAsync(partnerId);
    }

    public IQueryable<Partner> Query()
    {
        return _context.Partners;
    }

    public async Task AddAsync(Partner partner)
    {
        await _context.Partners.AddAsync(partner);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Partner partner)
    {
        _context.Partners.Update(partner);
        await _context.SaveChangesAsync();
    }
}