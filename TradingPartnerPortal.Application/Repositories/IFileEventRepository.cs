using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Repositories;

public interface IFileEventRepository
{
    IQueryable<FileTransferEvent> Query(Guid partnerId);
    Task<FileTransferEvent?> FindAsync(Guid partnerId, Guid fileId);
    Task AddAsync(FileTransferEvent fileEvent);
    Task UpdateAsync(FileTransferEvent fileEvent);
}