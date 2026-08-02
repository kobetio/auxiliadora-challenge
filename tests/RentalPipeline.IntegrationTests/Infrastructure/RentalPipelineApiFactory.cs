using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RentalPipeline.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API (<c>Program</c>) in-memory via <see cref="WebApplicationFactory{TEntryPoint}"/>,
/// backed by a real, ephemeral PostgreSQL instance started with Testcontainers — not an in-memory or
/// mocked database — so integration tests exercise the actual EF Core provider, migrations,
/// transactions, and optimistic/Serializable concurrency behavior described in Architecture.md.
/// One container is started per test run and shared by every test in <see cref="IntegrationTestCollection"/>
/// (see its class remarks for why).
/// </summary>
public class RentalPipelineApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("rentalpipeline_integration_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>The <see cref="IEventPublisher"/> test double registered for this host, so tests can
    /// assert on published events instead of only inferring them from log output.</summary>
    public RecordingEventPublisher EventPublisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher>(EventPublisher);
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Accessing Services builds the host lazily, running ConfigureWebHost above — by this point
        // the container is already started, so its real connection string is available. Program.cs
        // itself now also applies migrations automatically on startup, so this call is technically
        // redundant (MigrateAsync is idempotent) but kept explicit: it forces the host to build (and
        // the schema to exist) here in InitializeAsync, deterministically, rather than lazily on
        // whichever test happens to send the first request.
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RentalPipelineDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
