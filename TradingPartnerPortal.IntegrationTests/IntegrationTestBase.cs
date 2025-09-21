using FluentAssertions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TradingPartnerPortal.IntegrationTests;

/// <summary>
/// Base class for integration tests providing common setup and utilities.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<TestApplicationFactory>, IDisposable
{
    protected readonly TestApplicationFactory Factory;
    protected readonly HttpClient Client;
    private bool _disposed = false;

    protected IntegrationTestBase(TestApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        
        // Set default headers for testing
        SetAuthenticationToken("test-session-token");
    }

    /// <summary>
    /// Sets the authentication token for subsequent requests.
    /// </summary>
    protected void SetAuthenticationToken(string token)
    {
        Client.DefaultRequestHeaders.Remove("X-Session-Token");
        Client.DefaultRequestHeaders.Add("X-Session-Token", token);
    }

    /// <summary>
    /// Removes authentication token to test unauthorized scenarios.
    /// </summary>
    protected void ClearAuthenticationToken()
    {
        Client.DefaultRequestHeaders.Remove("X-Session-Token");
    }

    /// <summary>
    /// Creates a JSON HTTP content object from the provided object.
    /// </summary>
    protected static StringContent CreateJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Deserializes the response content to the specified type.
    /// </summary>
    protected static async Task<T> GetResponseContentAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }

    /// <summary>
    /// Asserts that the response has a successful status code and returns the content.
    /// </summary>
    protected static async Task<T> AssertSuccessAndGetContentAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Expected successful status code but got {response.StatusCode}. Content: {await response.Content.ReadAsStringAsync()}");
        
        return await GetResponseContentAsync<T>(response);
    }

    /// <summary>
    /// Seeds test data into the database before running tests.
    /// Override this method in derived classes to provide specific test data.
    /// </summary>
    protected virtual async Task SeedTestDataAsync()
    {
        // Default implementation - override in derived classes if needed
        await Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        if (!_disposed)
        {
            Client?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}