using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingPartnerPortal.Infrastructure.Data;
using TradingPartnerPortal.Infrastructure.Authentication;
using TradingPartnerPortal.Infrastructure.Middleware;

namespace TradingPartnerPortal.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// This sets up the test environment with a clean in-memory database for each test.
/// </summary>
public class TestApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string SharedDatabaseName = "SharedTestDb";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TradingPartnerPortalDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add a database context using a shared in-memory database for all tests
            // This ensures the middleware-seeded data is available to all tests
            services.AddDbContext<TradingPartnerPortalDbContext>(options =>
            {
                options.UseInMemoryDatabase(SharedDatabaseName);
            });

            // Ensure FakeAuthenticationService is registered as singleton for the test environment
            var authDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(FakeAuthenticationService));
            if (authDescriptor == null)
            {
                services.AddSingleton<FakeAuthenticationService>();
            }

            // Suppress logging during tests to reduce noise
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates a scope and returns the database context for test data setup.
    /// </summary>
    public TradingPartnerPortalDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TradingPartnerPortalDbContext>();
    }

    /// <summary>
    /// Seeds the database with test data.
    /// </summary>
    public async Task SeedTestDataAsync(Action<TradingPartnerPortalDbContext> seedAction)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradingPartnerPortalDbContext>();

        seedAction(context);
        await context.SaveChangesAsync();
    }
}