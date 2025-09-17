using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Infrastructure.Data;
using System.Collections.Concurrent;

namespace TradingPartnerPortal.Infrastructure.Repositories;

public class SseSequenceRepository : ISseSequenceRepository
{
    private readonly TradingPartnerPortalDbContext _context;
    private readonly ConcurrentDictionary<Guid, long> _sequences = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<SseEnvelope<object>>> _buffers = new();
    private const int MaxBufferSize = 500;

    public SseSequenceRepository(TradingPartnerPortalDbContext context)
    {
        _context = context;
    }

    public long GetNextSequence(Guid partnerId)
    {
        return _sequences.AddOrUpdate(partnerId, 1, (key, value) => value + 1);
    }

    public async Task UpdateLastSequenceAsync(Guid partnerId, long sequence)
    {
        var cursor = await _context.SseEventCursors.FindAsync(partnerId);
        if (cursor == null)
        {
            cursor = new SseEventCursor { PartnerId = partnerId, LastSequence = sequence };
            await _context.SseEventCursors.AddAsync(cursor);
        }
        else
        {
            cursor.LastSequence = sequence;
        }
        await _context.SaveChangesAsync();
    }

    public void Buffer(Guid partnerId, SseEnvelope<object> evt)
    {
        var buffer = _buffers.GetOrAdd(partnerId, _ => new ConcurrentQueue<SseEnvelope<object>>());
        buffer.Enqueue(evt);

        // Maintain buffer size limit
        while (buffer.Count > MaxBufferSize && buffer.TryDequeue(out _))
        {
            // Remove oldest events
        }
    }

    public IEnumerable<SseEnvelope<object>> GetBufferedEvents(Guid partnerId, long? fromSequence = null)
    {
        if (!_buffers.TryGetValue(partnerId, out var buffer))
            return Enumerable.Empty<SseEnvelope<object>>();

        var events = buffer.ToArray();
        
        if (fromSequence.HasValue)
        {
            return events.Where(e => e.Seq > fromSequence.Value);
        }

        return events;
    }
}