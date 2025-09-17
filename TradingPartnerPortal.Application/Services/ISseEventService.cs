using Microsoft.AspNetCore.Http;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Services;

public interface ISseEventService
{
    Task StreamAsync(HttpContext context, UserContext user, string? lastEventId);
    void PublishPartnerEvent<T>(Guid partnerId, string eventType, T data);
}

// Domain events for SSE publishing
public abstract class DomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public class KeyPromotedEvent : DomainEvent
{
    public Guid KeyId { get; set; }
    public Guid? PreviousPrimaryKeyId { get; set; }
}

public class KeyRevokedEvent : DomainEvent
{
    public Guid KeyId { get; set; }
}

public class FileCreatedEvent : DomainEvent
{
    public Guid FileId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string DocType { get; set; } = string.Empty;
}

public class FileStatusChangedEvent : DomainEvent
{
    public Guid FileId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
}