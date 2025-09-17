using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Repositories;

public interface IKeyRepository
{
    IQueryable<PgpKey> Query(Guid partnerId);
    Task<PgpKey?> FindAsync(Guid keyId);
    Task AddAsync(PgpKey key);
    Task UpdateAsync(PgpKey key);
    Task DeleteAsync(PgpKey key);
}