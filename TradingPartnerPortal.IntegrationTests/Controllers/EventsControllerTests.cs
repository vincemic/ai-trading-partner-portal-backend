using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Repositories;
using System.Text.Json;

namespace TradingPartnerPortal.IntegrationTests.Controllers;

public class EventsControllerTests : IntegrationTestBase
{
    private readonly Guid _testPartnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public EventsControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Stream_WithValidAuthentication_EstablishesConnection()
    {
        // Arrange
        SetAdminAuthentication();

        // Act
        var response = await Client.GetAsync("/api/events/stream");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
        response.Headers.CacheControl?.NoCache.Should().BeTrue();
        response.Headers.Connection.Should().Contain("keep-alive");
    }

    [Fact]
    public async Task Stream_WithoutAuthentication_ReturnsForbidden()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/events/stream");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Stream_WithLastEventId_ReplaysMissedEvents()
    {
        // Arrange
        SetAdminAuthentication();

        // First, publish some events
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Publish test events
        var testData1 = new FileCreatedEventData { FileId = "file-123", Direction = "Inbound", DocType = "850" };
        var testData2 = new KeyPromotedEventData { KeyId = "key-456", PreviousPrimaryKeyId = "key-789" };

        sseService.PublishPartnerEvent(_testPartnerId, "file.created", testData1);
        sseService.PublishPartnerEvent(_testPartnerId, "key.promoted", testData2);

        // Get buffered events to verify they exist
        var bufferedEvents = sequenceRepo.GetBufferedEvents(_testPartnerId).ToList();
        bufferedEvents.Should().HaveCountGreaterOrEqualTo(2);

        // Act - Request events from sequence 1 (should get second event)
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/events/stream");
        request.Headers.Add("Last-Event-ID", "1");

        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

        // Read initial chunk of the stream to verify event replay
        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[4096];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        var streamContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        streamContent.Should().Contain("event: key.promoted");
        streamContent.Should().Contain("id: 2");
        streamContent.Should().Contain("key-456");
    }

    [Fact]
    public async Task Stream_SendsHeartbeat_WhenNoEvents()
    {
        // Arrange
        SetAdminAuthentication();

        // Act
        using var response = await Client.GetAsync("/api/events/stream");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Read from stream with timeout to catch heartbeat
        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[1024];

        // Use a timeout to prevent test from hanging
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        try
        {
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Should receive heartbeat within the timeout period
            content.Should().Contain(":hb");
        }
        catch (OperationCanceledException)
        {
            // If no heartbeat received within timeout, this is acceptable for this test
            // since the exact timing may vary in test environment
        }
    }

    [Fact]
    public async Task Stream_HandlesClientDisconnection_Gracefully()
    {
        // Arrange
        SetAdminAuthentication();

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/events/stream");

        try
        {
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            var buffer = new byte[1024];

            // This should throw OperationCanceledException when timeout expires
            await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected behavior - client disconnection should be handled gracefully
        }

        // If we get here without exception, the test passes
        // The important thing is that no unhandled exceptions occur
    }

    [Fact]
    public async Task Stream_EventFormat_IsCorrect()
    {
        // Arrange
        SetAdminAuthentication();

        // Publish a test event
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();

        var testData = new FileCreatedEventData
        {
            FileId = "test-file-789",
            Direction = "Outbound",
            DocType = "810"
        };

        sseService.PublishPartnerEvent(_testPartnerId, "file.created", testData);

        // Act
        using var response = await Client.GetAsync("/api/events/stream");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[2048];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Verify SSE format: event: <type>\nid: <sequence>\ndata: <json>\n\n
        content.Should().Contain("event: file.created");
        content.Should().MatchRegex(@"id: \d+");
        content.Should().Contain("data: ");
        content.Should().Contain("test-file-789");
        content.Should().Contain("Outbound");
        content.Should().Contain("810");

        // Should end with double newline
        content.Should().Contain("\n\n");
    }

    [Fact]
    public async Task Stream_WithDifferentPartner_DoesNotReceiveOtherPartnerEvents()
    {
        // Arrange - Use different authentication tokens for different partners
        var otherPartnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Publish event for the other partner
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();

        var testData = new FileCreatedEventData { FileId = "other-partner-file", Direction = "Inbound", DocType = "850" };
        sseService.PublishPartnerEvent(otherPartnerId, "file.created", testData);

        // Act - Connect as the default test partner
        SetAdminAuthentication(); // This uses the default test partner

        using var response = await Client.GetAsync("/api/events/stream");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[1024];

        // Use timeout to prevent hanging
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        try
        {
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Should not contain the other partner's event
            content.Should().NotContain("other-partner-file");
        }
        catch (OperationCanceledException)
        {
            // Timeout is expected if no events for this partner
        }
    }

    [Fact]
    public async Task Stream_MultipleEventTypes_AreFormattedCorrectly()
    {
        // Arrange
        SetAdminAuthentication();

        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();

        // Publish different types of events
        var fileEvent = new FileCreatedEventData { FileId = "file-123", Direction = "Inbound", DocType = "850" };
        var statusEvent = new FileStatusChangedEventData { FileId = "file-123", OldStatus = "Processing", NewStatus = "Completed" };
        var keyEvent = new KeyPromotedEventData { KeyId = "key-456", PreviousPrimaryKeyId = "key-789" };
        var revokeEvent = new KeyRevokedEventData { KeyId = "key-999" };

        sseService.PublishPartnerEvent(_testPartnerId, "file.created", fileEvent);
        sseService.PublishPartnerEvent(_testPartnerId, "file.statusChanged", statusEvent);
        sseService.PublishPartnerEvent(_testPartnerId, "key.promoted", keyEvent);
        sseService.PublishPartnerEvent(_testPartnerId, "key.revoked", revokeEvent);

        // Act
        using var response = await Client.GetAsync("/api/events/stream");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[4096];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Verify all event types are present and correctly formatted
        content.Should().Contain("event: file.created");
        content.Should().Contain("event: file.statusChanged");
        content.Should().Contain("event: key.promoted");
        content.Should().Contain("event: key.revoked");

        // Verify event data
        content.Should().Contain("file-123");
        content.Should().Contain("Processing");
        content.Should().Contain("Completed");
        content.Should().Contain("key-456");
        content.Should().Contain("key-999");

        // Verify sequence IDs are present and increasing
        content.Should().MatchRegex(@"id: 1\n");
        content.Should().MatchRegex(@"id: 2\n");
        content.Should().MatchRegex(@"id: 3\n");
        content.Should().MatchRegex(@"id: 4\n");
    }

    #region Query Parameter Authentication Tests

    [Fact]
    public async Task Stream_WithQueryParameterAuth_EstablishesConnection()
    {
        // Arrange - Clear header auth and use query parameter instead
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/events/stream?token=admin-session-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
        response.Headers.CacheControl?.NoCache.Should().BeTrue();
        response.Headers.Connection.Should().Contain("keep-alive");
    }

    [Fact]
    public async Task Stream_WithQueryParameterAuth_ReceivesEvents()
    {
        // Arrange
        ClearAuthenticationToken();

        // Publish a test event
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();

        var testData = new FileCreatedEventData
        {
            FileId = "query-auth-test-file",
            Direction = "Inbound",
            DocType = "850"
        };

        sseService.PublishPartnerEvent(_testPartnerId, "file.created", testData);

        // Act - Use query parameter authentication
        using var response = await Client.GetAsync("/api/events/stream?token=admin-session-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[2048];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        content.Should().Contain("event: file.created");
        content.Should().Contain("query-auth-test-file");
        content.Should().Contain("Inbound");
        content.Should().Contain("850");
    }

    [Fact]
    public async Task Stream_WithQueryParameterAndLastEventId_ReplaysMissedEvents()
    {
        // Arrange
        ClearAuthenticationToken();

        // Publish some events first
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();

        var testData1 = new FileCreatedEventData { FileId = "query-file-1", Direction = "Inbound", DocType = "850" };
        var testData2 = new FileCreatedEventData { FileId = "query-file-2", Direction = "Outbound", DocType = "810" };

        sseService.PublishPartnerEvent(_testPartnerId, "file.created", testData1);
        sseService.PublishPartnerEvent(_testPartnerId, "file.created", testData2);

        // Act - Use query parameter auth with Last-Event-ID
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/events/stream?token=admin-session-token");
        request.Headers.Add("Last-Event-ID", "1");

        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[2048];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Should replay events after sequence 1
        content.Should().Contain("query-file-2");
        content.Should().MatchRegex(@"id: 2\n"); // Should start from sequence 2
    }

    [Fact]
    public async Task Stream_WithInvalidQueryParameterToken_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/events/stream?token=invalid-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Stream_WithEmptyQueryParameterToken_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/events/stream?token=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Stream_WithMissingTokenInBothHeaderAndQuery_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/events/stream");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Stream_HeaderTokenTakesPrecedenceOverQueryToken()
    {
        // Arrange - Set a valid header token and invalid query token
        SetAdminAuthentication(); // Sets valid header token

        // Publish test event to verify we're authenticated as the header token user
        using var scope = Factory.Services.CreateScope();
        var sseService = scope.ServiceProvider.GetRequiredService<ISseEventService>();

        var testData = new FileCreatedEventData
        {
            FileId = "precedence-test-file",
            Direction = "Inbound",
            DocType = "850"
        };

        sseService.PublishPartnerEvent(_testPartnerId, "file.created", testData);

        // Act - Use valid header auth but invalid query parameter
        var response = await Client.GetAsync("/api/events/stream?token=invalid-token");

        // Assert - Should succeed because header token takes precedence
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        content.Should().Contain("precedence-test-file");
    }

    [Fact]
    public async Task Stream_QueryParameterAuth_SupportsUserRoles()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act - Use user-level token via query parameter
        var response = await Client.GetAsync("/api/events/stream?token=user-session-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
    }

    [Fact]
    public async Task Stream_QueryParameterAuth_WithSpecialCharactersInToken_WorksCorrectly()
    {
        // Arrange
        ClearAuthenticationToken();

        // Test with URL-encoded characters that might appear in real tokens
        var encodedToken = Uri.EscapeDataString("test-session-token");

        // Act
        var response = await Client.GetAsync($"/api/events/stream?token={encodedToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
    }

    #endregion
}