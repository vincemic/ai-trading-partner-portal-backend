# Trading Partner Portal - Integration Tests

This project contains comprehensive functional/integration tests for the Trading Partner Portal API using xUnit and ASP.NET Core Test Host.

## Test Framework

- **xUnit** - Primary testing framework
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing with Test Host
- **FluentAssertions** - Enhanced assertions for better test readability
- **Entity Framework InMemory** - Isolated database for each test

## Test Structure

### Base Classes

- **`TestApplicationFactory`** - Custom WebApplicationFactory that sets up the test environment
- **`IntegrationTestBase`** - Base class for all tests with common utilities

### Test Categories

- **`AuthControllerTests`** - Authentication and authorization endpoints
- **`DashboardControllerTests`** - Dashboard metrics and analytics endpoints  
- **`KeysControllerTests`** - PGP key management operations
- **`SftpControllerTests`** - SFTP credential management

## Features

### Test Isolation
- Each test uses a unique in-memory database
- Independent test execution with proper cleanup
- No shared state between tests

### Authentication Testing
- Fake authentication system for testing different user roles
- Role-based access control validation
- Session token management

### Data Seeding
- Flexible test data setup through `SeedTestDataAsync`
- Realistic test scenarios with proper entity relationships
- Domain-specific test data for each controller

### Comprehensive Coverage
- Happy path scenarios
- Error conditions and edge cases
- Authorization and permission checks
- Input validation testing
- HTTP status code verification

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "ClassName=AuthControllerTests"

# Run tests with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage"
```

## Test Examples

### Basic Controller Test
```csharp
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
}
```

### Authorization Test
```csharp
[Fact]
public async Task UploadKey_WithRegularUserRole_ReturnsForbidden()
{
    // Arrange
    SetAuthenticationToken(_userSessionToken);
    var request = new UploadKeyRequest { /* ... */ };

    // Act
    var response = await Client.PostAsync("/api/keys/upload", CreateJsonContent(request));

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

## Best Practices

1. **Test Naming**: Use descriptive names that clearly indicate what is being tested
2. **Arrange-Act-Assert**: Follow the AAA pattern for test structure
3. **Test Independence**: Each test should be able to run in isolation
4. **Realistic Data**: Use domain-appropriate test data
5. **Error Testing**: Include both positive and negative test cases
6. **Clean Code**: Keep tests simple and focused on single behaviors

## Configuration

The test project automatically:
- Uses a testing environment configuration
- Suppresses logging noise during test execution
- Sets up unique databases for test isolation
- Configures all necessary dependencies through DI

## Debugging

Tests can be debugged normally in Visual Studio or VS Code:
- Set breakpoints in test methods
- Inspect HTTP responses and database state
- Step through authentication and authorization logic
- Examine test data seeding and cleanup