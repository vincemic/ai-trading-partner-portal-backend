using Microsoft.EntityFrameworkCore;
using Serilog;
using TradingPartnerPortal.Application.Repositories;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Infrastructure.Authentication;
using TradingPartnerPortal.Infrastructure.Data;
using TradingPartnerPortal.Infrastructure.Middleware;
using TradingPartnerPortal.Infrastructure.Repositories;
using TradingPartnerPortal.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/trading-partner-portal-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// Configure Entity Framework with InMemory database
builder.Services.AddDbContext<TradingPartnerPortalDbContext>(options =>
    options.UseInMemoryDatabase("TradingPartnerPortal"));

// Register repositories
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<IKeyRepository, KeyRepository>();
builder.Services.AddScoped<ISftpCredentialRepository, SftpCredentialRepository>();
builder.Services.AddScoped<IFileEventRepository, FileEventRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<ISseSequenceRepository, SseSequenceRepository>();
builder.Services.AddScoped<IConnectionEventRepository, ConnectionEventRepository>();

// Register services
builder.Services.AddScoped<IKeyService, KeyService>();
builder.Services.AddScoped<ISftpCredentialService, SftpCredentialService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileEventService, FileEventService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISseEventService, SseEventService>();

// Register fake authentication
builder.Services.AddSingleton<FakeAuthenticationService>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Trading Partner Portal API", Version = "v1" });
    c.AddSecurityDefinition("SessionToken", new()
    {
        Name = "X-Session-Token",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Session token for fake authentication"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "SessionToken" }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

// Use fake authentication middleware
app.UseMiddleware<FakeAuthenticationMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Seed sample data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TradingPartnerPortalDbContext>();
    await SeedSampleData(context);
}

app.Run();

static async Task SeedSampleData(TradingPartnerPortalDbContext context)
{
    // Implementation will be added in the next iteration
    await Task.CompletedTask;
}

// Make Program class accessible for testing
public partial class Program { }