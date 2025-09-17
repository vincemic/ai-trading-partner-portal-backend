using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Services;

public interface IKeyService
{
    Task<IReadOnlyList<KeySummaryDto>> ListAsync(Guid partnerId);
    Task<(KeySummaryDto key, AuditEvent audit)> UploadAsync(Guid partnerId, UploadKeyRequest request, UserContext user);
    Task<(GenerateKeyResponse response, AuditEvent audit)> GenerateAsync(Guid partnerId, GenerateKeyRequest request, UserContext user);
    Task<AuditEvent> RevokeAsync(Guid partnerId, Guid keyId, RevokeKeyRequest request, UserContext user);
    Task<AuditEvent?> PromoteAsync(Guid partnerId, Guid keyId, UserContext user);
}