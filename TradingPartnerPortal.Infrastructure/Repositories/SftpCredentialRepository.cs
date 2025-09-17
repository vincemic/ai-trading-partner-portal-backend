using Microsoft.EntityFrameworkCore;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Infrastructure.Data;

namespace TradingPartnerPortal.Infrastructure.Repositories;

public class SftpCredentialRepository : ISftpCredentialRepository
{
    private readonly TradingPartnerPortalDbContext _context;

    public SftpCredentialRepository(TradingPartnerPortalDbContext context)
    {
        _context = context;
    }

    public async Task<SftpCredential?> FindAsync(Guid partnerId)
    {
        return await _context.SftpCredentials.FindAsync(partnerId);
    }

    public async Task UpsertAsync(SftpCredential credential)
    {
        var existing = await FindAsync(credential.PartnerId);
        if (existing == null)
        {
            await _context.SftpCredentials.AddAsync(credential);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(credential);
        }
        await _context.SaveChangesAsync();
    }
}