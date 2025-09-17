using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Services;

public interface ISftpCredentialService
{
    Task<SftpCredentialMetadataDto?> GetMetadataAsync(Guid partnerId);
    Task<(RotatePasswordResponse response, AuditEvent audit)> RotateAsync(Guid partnerId, RotatePasswordRequest request, UserContext user);
}