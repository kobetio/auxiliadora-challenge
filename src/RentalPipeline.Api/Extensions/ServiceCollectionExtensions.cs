using Microsoft.EntityFrameworkCore;
using RentalPipeline.Infrastructure.Persistence;

namespace RentalPipeline.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the EF Core <see cref="RentalPipelineDbContext"/> using the PostgreSQL provider.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<RentalPipelineDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
