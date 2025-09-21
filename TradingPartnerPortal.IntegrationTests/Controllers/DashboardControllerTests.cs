using System.Net;
using FluentAssertions;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Domain.Entities;
using TradingPartnerPortal.Domain.Enums;

namespace TradingPartnerPortal.IntegrationTests.Controllers;

public class DashboardControllerTests : IntegrationTestBase
{
    public DashboardControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetSummary_WithValidSession_ReturnsDashboardSummary()
    {
        // Arrange - Use middleware-seeded data
        SetUserAuthentication();

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
        // Arrange - Use middleware-seeded data
        SetUserAuthentication();

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
        // Arrange - Use middleware-seeded data
        SetUserAuthentication();
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
        // Arrange - Use middleware-seeded data
        SetUserAuthentication();

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
        // Arrange - Use middleware-seeded data
        SetUserAuthentication();
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
        // Arrange - Use middleware-seeded data
        SetUserAuthentication();

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
        // Arrange - Use middleware-seeded data
        SetUserAuthentication();
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