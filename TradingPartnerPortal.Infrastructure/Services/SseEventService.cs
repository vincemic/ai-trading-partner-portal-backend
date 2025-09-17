using Microsoft.AspNetCore.Http;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Application.Services;
using System.Text;

namespace TradingPartnerPortal.Infrastructure.Services;

public class SseEventService : ISseEventService
{
    private readonly ISseSequenceRepository _sequenceRepository;

    public SseEventService(ISseSequenceRepository sequenceRepository)
    {
        _sequenceRepository = sequenceRepository;
    }

    public async Task StreamAsync(HttpContext context, UserContext user, string? lastEventId)
    {
        context.Response.Headers.Add("Content-Type", "text/event-stream");
        context.Response.Headers.Add("Cache-Control", "no-cache");
        context.Response.Headers.Add("Connection", "keep-alive");

        // Parse last event ID
        long? fromSequence = null;
        if (!string.IsNullOrEmpty(lastEventId) && long.TryParse(lastEventId, out var lastSeq))
        {
            fromSequence = lastSeq;
        }

        // Send buffered events
        var bufferedEvents = _sequenceRepository.GetBufferedEvents(user.PartnerId, fromSequence);
        foreach (var evt in bufferedEvents)
        {
            await WriteEvent(context.Response, evt.Type, evt.Data, evt.Seq.ToString());
        }

        // Keep connection alive with heartbeats
        var cancellationToken = context.RequestAborted;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(15000, cancellationToken); // 15 second heartbeat
                await WriteHeartbeat(context.Response);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
    }

    public void PublishPartnerEvent<T>(Guid partnerId, string eventType, T data)
    {
        var sequence = _sequenceRepository.GetNextSequence(partnerId);
        var envelope = new SseEnvelope<object>
        {
            Seq = sequence,
            Type = eventType,
            OccurredAt = DateTime.UtcNow,
            Data = data!
        };

        _sequenceRepository.Buffer(partnerId, envelope);
    }

    private static async Task WriteEvent(HttpResponse response, string eventType, object data, string id)
    {
        var eventData = $"event: {eventType}\nid: {id}\ndata: {System.Text.Json.JsonSerializer.Serialize(data)}\n\n";
        await response.WriteAsync(eventData, Encoding.UTF8);
        await response.Body.FlushAsync();
    }

    private static async Task WriteHeartbeat(HttpResponse response)
    {
        await response.WriteAsync(":hb\n\n", Encoding.UTF8);
        await response.Body.FlushAsync();
    }
}