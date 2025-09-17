using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Application.Repositories;

public interface ISseSequenceRepository
{
    long GetNextSequence(Guid partnerId);
    Task UpdateLastSequenceAsync(Guid partnerId, long sequence);
    void Buffer(Guid partnerId, SseEnvelope<object> evt);
    IEnumerable<SseEnvelope<object>> GetBufferedEvents(Guid partnerId, long? fromSequence = null);
}

public class SseEnvelope<T>
{
    public long Seq { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public T Data { get; set; } = default!;
}