using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Repositories;

public interface IPartnerRepository
{
    Task<Partner?> FindAsync(Guid partnerId);
    IQueryable<Partner> Query();
    Task AddAsync(Partner partner);
    Task UpdateAsync(Partner partner);
}