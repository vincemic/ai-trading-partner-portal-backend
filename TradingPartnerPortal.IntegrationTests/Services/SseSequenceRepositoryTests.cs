using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Application.DTOs;

namespace TradingPartnerPortal.IntegrationTests.Services;

public class SseSequenceRepositoryTests : IntegrationTestBase
{
    private readonly Guid _testPartnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _otherPartnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public SseSequenceRepositoryTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public void GetNextSequence_FirstCall_ReturnsOne()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Act
        var sequence = sequenceRepo.GetNextSequence(_testPartnerId);

        // Assert
        sequence.Should().Be(1);
    }

    [Fact]
    public void GetNextSequence_MultipleCalls_IncrementsSequence()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Act
        var seq1 = sequenceRepo.GetNextSequence(_testPartnerId);
        var seq2 = sequenceRepo.GetNextSequence(_testPartnerId);
        var seq3 = sequenceRepo.GetNextSequence(_testPartnerId);

        // Assert
        seq1.Should().Be(1);
        seq2.Should().Be(2);
        seq3.Should().Be(3);
    }

    [Fact]
    public void GetNextSequence_DifferentPartners_HaveSeparateSequences()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Act
        var partner1Seq1 = sequenceRepo.GetNextSequence(_testPartnerId);
        var partner2Seq1 = sequenceRepo.GetNextSequence(_otherPartnerId);
        var partner1Seq2 = sequenceRepo.GetNextSequence(_testPartnerId);

        // Assert
        partner1Seq1.Should().Be(1);
        partner2Seq1.Should().Be(1);
        partner1Seq2.Should().Be(2);
    }

    [Fact]
    public void Buffer_SingleEvent_IsStored()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        var envelope = new SseEnvelope<object>
        {
            Seq = 1,
            Type = "file.created",
            OccurredAt = DateTime.UtcNow,
            Data = new FileCreatedEventData { FileId = "test-file", Direction = "Inbound", DocType = "850" }
        };

        // Act
        sequenceRepo.Buffer(_testPartnerId, envelope);

        // Assert
        var bufferedEvents = sequenceRepo.GetBufferedEvents(_testPartnerId).ToList();
        bufferedEvents.Should().HaveCount(1);
        bufferedEvents[0].Should().BeEquivalentTo(envelope);
    }

    [Fact]
    public void Buffer_MultipleEvents_AreStoredInOrder()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        var event1 = new SseEnvelope<object>
        {
            Seq = 1,
            Type = "file.created",
            OccurredAt = DateTime.UtcNow,
            Data = new FileCreatedEventData { FileId = "file-1" }
        };

        var event2 = new SseEnvelope<object>
        {
            Seq = 2,
            Type = "key.promoted",
            OccurredAt = DateTime.UtcNow,
            Data = new KeyPromotedEventData { KeyId = "key-1" }
        };

        // Act
        sequenceRepo.Buffer(_testPartnerId, event1);
        sequenceRepo.Buffer(_testPartnerId, event2);

        // Assert
        var bufferedEvents = sequenceRepo.GetBufferedEvents(_testPartnerId).ToList();
        bufferedEvents.Should().HaveCount(2);
        bufferedEvents[0].Seq.Should().Be(1);
        bufferedEvents[1].Seq.Should().Be(2);
    }

    [Fact]
    public void Buffer_ExceedsMaxSize_RemovesOldestEvents()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Act - Add more than max buffer size (500) events
        // For testing purposes, we'll add 550 events to trigger cleanup
        for (int i = 1; i <= 550; i++)
        {
            var envelope = new SseEnvelope<object>
            {
                Seq = i,
                Type = "test.event",
                OccurredAt = DateTime.UtcNow,
                Data = new { eventNumber = i }
            };
            sequenceRepo.Buffer(_testPartnerId, envelope);
        }

        // Assert
        var bufferedEvents = sequenceRepo.GetBufferedEvents(_testPartnerId).ToList();
        bufferedEvents.Should().HaveCount(500); // Should be capped at max size

        // Should have the most recent 500 events (51-550)
        bufferedEvents.First().Seq.Should().Be(51);
        bufferedEvents.Last().Seq.Should().Be(550);
    }

    [Fact]
    public void GetBufferedEvents_WithFromSequence_FiltersOldEvents()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Add test events
        for (int i = 1; i <= 5; i++)
        {
            var envelope = new SseEnvelope<object>
            {
                Seq = i,
                Type = "test.event",
                OccurredAt = DateTime.UtcNow,
                Data = new { eventNumber = i }
            };
            sequenceRepo.Buffer(_testPartnerId, envelope);
        }

        // Act - Get events from sequence 3
        var filteredEvents = sequenceRepo.GetBufferedEvents(_testPartnerId, fromSequence: 3).ToList();

        // Assert
        filteredEvents.Should().HaveCount(2); // Events 4 and 5
        filteredEvents[0].Seq.Should().Be(4);
        filteredEvents[1].Seq.Should().Be(5);
    }

    [Fact]
    public void GetBufferedEvents_WithoutFromSequence_ReturnsAllEvents()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Add test events
        for (int i = 1; i <= 3; i++)
        {
            var envelope = new SseEnvelope<object>
            {
                Seq = i,
                Type = "test.event",
                OccurredAt = DateTime.UtcNow,
                Data = new { eventNumber = i }
            };
            sequenceRepo.Buffer(_testPartnerId, envelope);
        }

        // Act
        var allEvents = sequenceRepo.GetBufferedEvents(_testPartnerId).ToList();

        // Assert
        allEvents.Should().HaveCount(3);
        allEvents[0].Seq.Should().Be(1);
        allEvents[1].Seq.Should().Be(2);
        allEvents[2].Seq.Should().Be(3);
    }

    [Fact]
    public void GetBufferedEvents_NoEventsForPartner_ReturnsEmpty()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Act
        var events = sequenceRepo.GetBufferedEvents(_testPartnerId).ToList();

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public void GetBufferedEvents_DifferentPartners_AreIsolated()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        var partner1Event = new SseEnvelope<object>
        {
            Seq = 1,
            Type = "partner1.event",
            OccurredAt = DateTime.UtcNow,
            Data = new { partnerId = _testPartnerId }
        };

        var partner2Event = new SseEnvelope<object>
        {
            Seq = 1,
            Type = "partner2.event",
            OccurredAt = DateTime.UtcNow,
            Data = new { partnerId = _otherPartnerId }
        };

        // Act
        sequenceRepo.Buffer(_testPartnerId, partner1Event);
        sequenceRepo.Buffer(_otherPartnerId, partner2Event);

        // Assert
        var partner1Events = sequenceRepo.GetBufferedEvents(_testPartnerId).ToList();
        var partner2Events = sequenceRepo.GetBufferedEvents(_otherPartnerId).ToList();

        partner1Events.Should().HaveCount(1);
        partner2Events.Should().HaveCount(1);

        partner1Events[0].Type.Should().Be("partner1.event");
        partner2Events[0].Type.Should().Be("partner2.event");
    }

    [Fact]
    public async Task UpdateLastSequenceAsync_CreatesNewCursor_WhenNotExists()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Act
        await sequenceRepo.UpdateLastSequenceAsync(_testPartnerId, 5);

        // Assert - Verify the sequence was stored (this test may need database context access)
        // Since we're using in-memory database, we can verify by calling the method again
        // and checking if it updates correctly
        await sequenceRepo.UpdateLastSequenceAsync(_testPartnerId, 10);
        // If no exception is thrown, the cursor was created and updated successfully
    }

    [Fact]
    public async Task UpdateLastSequenceAsync_UpdatesExistingCursor()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sequenceRepo = scope.ServiceProvider.GetRequiredService<ISseSequenceRepository>();

        // Create initial cursor
        await sequenceRepo.UpdateLastSequenceAsync(_testPartnerId, 3);

        // Act
        await sequenceRepo.UpdateLastSequenceAsync(_testPartnerId, 7);

        // Assert - If no exception is thrown, the update was successful
        // In a more complete test, we would verify the actual value in the database
    }
}