using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Infrastructure.Services;
using System.IO;
using System.Text;

namespace TradingPartnerPortal.IntegrationTests.Services;

public class SseEventServiceTests : IntegrationTestBase
{
    private readonly Guid _testPartnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _otherPartnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public SseEventServiceTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public void PublishPartnerEvent_CreatesEventWithSequence()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        var testData = new FileCreatedEventData
        {
            FileId = "test-file-123",
            Direction = "Inbound",
            DocType = "850"
        };

        // Act
        sseService.PublishPartnerEvent(_testPartnerId, "file.created", testData);

        // Assert
        var bufferedEvents = sequenceRepo.GetBufferedEvents(_testPartnerId).ToList();
        bufferedEvents.Should().HaveCount(1);

        var evt = bufferedEvents.First();
        evt.Seq.Should().BeGreaterThan(0);
        evt.Type.Should().Be("file.created");
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify data can be cast back
        var eventDataJson = System.Text.Json.JsonSerializer.Serialize(evt.Data);
        eventDataJson.Should().Contain("test-file-123");
        eventDataJson.Should().Contain("Inbound");
        eventDataJson.Should().Contain("850");
    }

    [Fact]
    public void PublishPartnerEvent_MultipleEvents_IncrementsSequence()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Act
        sseService.PublishPartnerEvent(_testPartnerId, "file.created", new FileCreatedEventData { FileId = "file-1" });
        sseService.PublishPartnerEvent(_testPartnerId, "file.created", new FileCreatedEventData { FileId = "file-2" });
        sseService.PublishPartnerEvent(_testPartnerId, "key.promoted", new KeyPromotedEventData { KeyId = "key-1" });

        // Assert
        var bufferedEvents = sequenceRepo.GetBufferedEvents(_testPartnerId).OrderBy(e => e.Seq).ToList();
        bufferedEvents.Should().HaveCount(3);

        bufferedEvents[0].Seq.Should().Be(1);
        bufferedEvents[1].Seq.Should().Be(2);
        bufferedEvents[2].Seq.Should().Be(3);

        bufferedEvents[0].Type.Should().Be("file.created");
        bufferedEvents[1].Type.Should().Be("file.created");
        bufferedEvents[2].Type.Should().Be("key.promoted");
    }

    [Fact]
    public void PublishPartnerEvent_DifferentPartners_HaveSeparateSequences()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Act
        sseService.PublishPartnerEvent(_testPartnerId, "file.created", new FileCreatedEventData { FileId = "partner1-file" });
        sseService.PublishPartnerEvent(_otherPartnerId, "file.created", new FileCreatedEventData { FileId = "partner2-file" });
        sseService.PublishPartnerEvent(_testPartnerId, "key.promoted", new KeyPromotedEventData { KeyId = "partner1-key" });

        // Assert
        var partner1Events = sequenceRepo.GetBufferedEvents(_testPartnerId).OrderBy(e => e.Seq).ToList();
        var partner2Events = sequenceRepo.GetBufferedEvents(_otherPartnerId).OrderBy(e => e.Seq).ToList();

        partner1Events.Should().HaveCount(2);
        partner2Events.Should().HaveCount(1);

        // Each partner should have their own sequence starting from 1
        partner1Events[0].Seq.Should().Be(1);
        partner1Events[1].Seq.Should().Be(2);
        partner2Events[0].Seq.Should().Be(1);

        // Verify events are partner-specific
        var partner1Data = partner1Events.Select(e => System.Text.Json.JsonSerializer.Serialize(e.Data)).ToList();
        var partner2Data = partner2Events.Select(e => System.Text.Json.JsonSerializer.Serialize(e.Data)).ToList();

        partner1Data.Should().OnlyContain(data => data.Contains("partner1"));
        partner2Data.Should().OnlyContain(data => data.Contains("partner2"));
    }

    [Fact]
    public async Task StreamAsync_WritesCorrectHeaders()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var userContext = new UserContext
        {
            PartnerId = _testPartnerId,
            UserId = Guid.NewGuid().ToString(),
            Role = "PartnerAdmin"
        };

        // Act & Assert - This will run until cancellation
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(1)); // Cancel after 1 second
        httpContext.RequestAborted = cancellationTokenSource.Token;

        try
        {
            await sseService.StreamAsync(httpContext, userContext, null);
        }
        catch (OperationCanceledException)
        {
            // Expected due to cancellation
        }

        // Assert headers were set
        httpContext.Response.Headers["Content-Type"].ToString().Should().Be("text/event-stream");
        httpContext.Response.Headers["Cache-Control"].ToString().Should().Be("no-cache");
        httpContext.Response.Headers["Connection"].ToString().Should().Be("keep-alive");
    }

    [Fact]
    public async Task StreamAsync_WithBufferedEvents_WritesEvents()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Publish events first
        var testData = new FileCreatedEventData { FileId = "buffered-file", Direction = "Outbound", DocType = "810" };
        sseService.PublishPartnerEvent(_testPartnerId, "file.created", testData);

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var userContext = new UserContext
        {
            PartnerId = _testPartnerId,
            UserId = Guid.NewGuid().ToString(),
            Role = "PartnerAdmin"
        };

        // Act - Cancel quickly to capture initial buffered events
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(500));
        httpContext.RequestAborted = cancellationTokenSource.Token;

        try
        {
            await sseService.StreamAsync(httpContext, userContext, null);
        }
        catch (OperationCanceledException)
        {
            // Expected due to cancellation
        }

        // Assert
        responseStream.Position = 0;
        var reader = new StreamReader(responseStream);
        var content = await reader.ReadToEndAsync();

        content.Should().Contain("event: file.created");
        content.Should().Contain("id: 1");
        content.Should().Contain("data:");
        content.Should().Contain("buffered-file");
        content.Should().Contain("Outbound");
        content.Should().Contain("810");
    }

    [Fact]
    public async Task StreamAsync_WithLastEventId_SkipsOldEvents()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();

        // Publish multiple events
        sseService.PublishPartnerEvent(_testPartnerId, "file.created", new FileCreatedEventData { FileId = "old-file" });
        sseService.PublishPartnerEvent(_testPartnerId, "file.created", new FileCreatedEventData { FileId = "new-file" });

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var userContext = new UserContext
        {
            PartnerId = _testPartnerId,
            UserId = Guid.NewGuid().ToString(),
            Role = "PartnerAdmin"
        };

        // Act - Request from sequence 1 (should only get sequence 2)
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(500));
        httpContext.RequestAborted = cancellationTokenSource.Token;

        try
        {
            await sseService.StreamAsync(httpContext, userContext, "1");
        }
        catch (OperationCanceledException)
        {
            // Expected due to cancellation
        }

        // Assert
        responseStream.Position = 0;
        var reader = new StreamReader(responseStream);
        var content = await reader.ReadToEndAsync();

        content.Should().NotContain("old-file");
        content.Should().Contain("new-file");
        content.Should().Contain("id: 2");
    }

    [Fact]
    public async Task StreamAsync_SendsHeartbeat_WhenIdle()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var userContext = new UserContext
        {
            PartnerId = _testPartnerId,
            UserId = Guid.NewGuid().ToString(),
            Role = "PartnerAdmin"
        };

        // Act - Let it run long enough to get a heartbeat (but not the full 15 seconds)
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(16)); // Slightly longer than heartbeat interval
        httpContext.RequestAborted = cancellationTokenSource.Token;

        try
        {
            await sseService.StreamAsync(httpContext, userContext, null);
        }
        catch (OperationCanceledException)
        {
            // Expected due to cancellation
        }

        // Assert
        responseStream.Position = 0;
        var reader = new StreamReader(responseStream);
        var content = await reader.ReadToEndAsync();

        content.Should().Contain(":hb");
    }

    [Fact]
    public void PublishPartnerEvent_AllEventTypes_AreHandledCorrectly()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Act - Publish all event types
        var fileCreatedData = new FileCreatedEventData { FileId = "file-123", Direction = "Inbound", DocType = "850" };
        var fileStatusData = new FileStatusChangedEventData { FileId = "file-123", OldStatus = "Processing", NewStatus = "Completed" };
        var keyPromotedData = new KeyPromotedEventData { KeyId = "key-456", PreviousPrimaryKeyId = "key-789" };
        var keyRevokedData = new KeyRevokedEventData { KeyId = "key-999" };
        var dashboardData = new DashboardMetricsTickData { Summary = new DashboardSummaryDto() };

        sseService.PublishPartnerEvent(_testPartnerId, "file.created", fileCreatedData);
        sseService.PublishPartnerEvent(_testPartnerId, "file.statusChanged", fileStatusData);
        sseService.PublishPartnerEvent(_testPartnerId, "key.promoted", keyPromotedData);
        sseService.PublishPartnerEvent(_testPartnerId, "key.revoked", keyRevokedData);
        sseService.PublishPartnerEvent(_testPartnerId, "dashboard.metricsTick", dashboardData);

        // Assert
        var bufferedEvents = sequenceRepo.GetBufferedEvents(_testPartnerId).OrderBy(e => e.Seq).ToList();
        bufferedEvents.Should().HaveCount(5);

        bufferedEvents[0].Type.Should().Be("file.created");
        bufferedEvents[1].Type.Should().Be("file.statusChanged");
        bufferedEvents[2].Type.Should().Be("key.promoted");
        bufferedEvents[3].Type.Should().Be("key.revoked");
        bufferedEvents[4].Type.Should().Be("dashboard.metricsTick");

        // Verify all events have proper sequence and timestamp
        for (int i = 0; i < bufferedEvents.Count; i++)
        {
            bufferedEvents[i].Seq.Should().Be(i + 1);
            bufferedEvents[i].OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            bufferedEvents[i].Data.Should().NotBeNull();
        }
    }
}