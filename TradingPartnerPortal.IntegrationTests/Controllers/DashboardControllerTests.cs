using System.Net;
using FluentAssertions;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.IntegrationTests.Controllers;

public class DashboardControllerTests : IntegrationTestBase
{
    // Use the default test partner ID that matches the middleware
    private readonly Guid _testPartnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public DashboardControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    protected override async Task SeedTestDataAsync()
    {
        // Set user authentication for seeding
        SetUserAuthentication();

        // Seed test data
        await Factory.SeedTestDataAsync(context =>
        {
            // Add test partner
            var partner = new Partner
            {
                PartnerId = _testPartnerId,
                Name = "Test Partner",
                Status = PartnerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };
            context.Partners.Add(partner);

            // Add some test file events
            var baseTime = DateTime.UtcNow.AddHours(-12);

            for (int i = 0; i < 10; i++)
            {
                var fileEvent = new FileTransferEvent
                {
                    FileId = Guid.NewGuid(),
                    PartnerId = _testPartnerId,
                    DocType = $"test-file-{i}.txt",
                    Direction = i % 2 == 0 ? FileDirection.Inbound : FileDirection.Outbound,
                    Status = i < 8 ? FileStatus.Success : FileStatus.Failed,
                    SizeBytes = 1000 + (i * 100),
                    ReceivedAt = baseTime.AddHours(i),
                    ProcessedAt = i < 8 ? baseTime.AddHours(i).AddMinutes(5) : null,
                    ErrorMessage = i >= 8 ? "Test error message" : null,
                    CorrelationId = Guid.NewGuid().ToString()
                };
                context.FileTransferEvents.Add(fileEvent);
            }

            // Add some SFTP connection events
            for (int i = 0; i < 5; i++)
            {
                var connectionEvent = new SftpConnectionEvent
                {
                    EventId = Guid.NewGuid(),
                    PartnerId = _testPartnerId,
                    Outcome = i < 4 ? ConnectionOutcome.Success : ConnectionOutcome.Failed,
                    OccurredAt = baseTime.AddHours(i * 2),
                    ConnectionTimeMs = 100 + (i * 50)
                };
                context.SftpConnectionEvents.Add(connectionEvent);
            }
        });
    }

    [Fact]
    public async Task GetSummary_WithValidSession_ReturnsDashboardSummary()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await GetResponseContentAsync<DashboardSummaryDto>(response);
        summary.Should().NotBeNull();
        summary.InboundFiles24h.Should().BeGreaterOrEqualTo(0);
        summary.OutboundFiles24h.Should().BeGreaterOrEqualTo(0);
        summary.SuccessRate.Should().BeInRange(0, 100);
        summary.AvgProcessingTime.Should().BeGreaterOrEqualTo(0);
        summary.OpenErrors.Should().BeGreaterOrEqualTo(0);
        summary.TotalBytes24h.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetSummary_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/dashboard/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTimeSeries_WithValidSession_ReturnsTimeSeriesData()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard/timeseries");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var timeSeries = await GetResponseContentAsync<TimeSeriesResponse>(response);
        timeSeries.Should().NotBeNull();
        timeSeries.Points.Should().NotBeNull();
        // Points might be empty if no data in the time range, but the structure should be correct
    }

    [Fact]
    public async Task GetTimeSeries_WithCustomDateRange_ReturnsFilteredData()
    {
        // Arrange
        await SeedTestDataAsync();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;

        // Act
        var response = await Client.GetAsync($"/api/dashboard/timeseries?from={from:O}&to={to:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var timeSeries = await GetResponseContentAsync<TimeSeriesResponse>(response);
        timeSeries.Should().NotBeNull();
        timeSeries.Points.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeSeries_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/dashboard/timeseries");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTopErrors_WithValidSession_ReturnsTopErrors()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard/errors/top");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var topErrors = await GetResponseContentAsync<TopErrorsResponse>(response);
        topErrors.Should().NotBeNull();
        topErrors.Categories.Should().NotBeNull();
        // Categories might be empty if no errors, but the structure should be correct
    }

    [Fact]
    public async Task GetTopErrors_WithCustomParameters_ReturnsFilteredErrors()
    {
        // Arrange
        await SeedTestDataAsync();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        var top = 10;

        // Act
        var response = await Client.GetAsync($"/api/dashboard/errors/top?from={from:O}&to={to:O}&top={top}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var topErrors = await GetResponseContentAsync<TopErrorsResponse>(response);
        topErrors.Should().NotBeNull();
        topErrors.Categories.Should().NotBeNull();
        topErrors.Categories.Count.Should().BeLessOrEqualTo(top);
    }

    [Fact]
    public async Task GetTopErrors_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationToken();

        // Act
        var response = await Client.GetAsync("/api/dashboard/errors/top");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetTopErrors_WithInvalidTopParameter_ReturnsDefaultResults(int invalidTop)
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync($"/api/dashboard/errors/top?top={invalidTop}");

        // Assert
        // The API should handle invalid parameters gracefully
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var topErrors = await GetResponseContentAsync<TopErrorsResponse>(response);
            topErrors.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task GetTimeSeries_WithInvalidDateRange_ReturnsAppropriateResponse()
    {
        // Arrange
        await SeedTestDataAsync();
        var from = DateTime.UtcNow;
        var to = DateTime.UtcNow.AddDays(-1); // 'to' is before 'from'

        // Act
        var response = await Client.GetAsync($"/api/dashboard/timeseries?from={from:O}&to={to:O}");

        // Assert
        // The API should handle invalid date ranges gracefully
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var timeSeries = await GetResponseContentAsync<TimeSeriesResponse>(response);
            timeSeries.Should().NotBeNull();
            timeSeries.Points.Should().BeEmpty();
        }
    }
}