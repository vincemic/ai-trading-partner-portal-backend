using Microsoft.EntityFrameworkCore;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Infrastructure.Data;

namespace TradingPartnerPortal.Infrastructure.Repositories;

public class FileEventRepository : IFileEventRepository
{
    private readonly TradingPartnerPortalDbContext _context;

    public FileEventRepository(TradingPartnerPortalDbContext context)
    {
        _context = context;
    }

    public IQueryable<FileTransferEvent> Query(Guid partnerId)
    {
        return _context.FileTransferEvents.Where(f => f.PartnerId == partnerId);
    }

    public async Task<FileTransferEvent?> FindAsync(Guid partnerId, Guid fileId)
    {
        return await _context.FileTransferEvents
            .FirstOrDefaultAsync(f => f.PartnerId == partnerId && f.FileId == fileId);
    }

    public async Task AddAsync(FileTransferEvent fileEvent)
    {
        await _context.FileTransferEvents.AddAsync(fileEvent);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FileTransferEvent fileEvent)
    {
        _context.FileTransferEvents.Update(fileEvent);
        await _context.SaveChangesAsync();
    }
}