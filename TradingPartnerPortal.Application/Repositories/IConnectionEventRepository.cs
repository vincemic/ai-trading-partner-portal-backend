using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Repositories;

public interface IConnectionEventRepository
{
    IQueryable<SftpConnectionEvent> Query(Guid partnerId);
    Task AddAsync(SftpConnectionEvent connectionEvent);
}