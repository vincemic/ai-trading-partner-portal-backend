using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Repositories;

public interface ISftpCredentialRepository
{
    Task<SftpCredential?> FindAsync(Guid partnerId);
    Task UpsertAsync(SftpCredential credential);
}