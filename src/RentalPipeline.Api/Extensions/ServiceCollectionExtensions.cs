using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Application.Services;
using RentalPipeline.Application.Validators;
using RentalPipeline.Domain.Interfaces;
using RentalPipeline.Infrastructure.Persistence;
using RentalPipeline.Infrastructure.Repositories;

namespace RentalPipeline.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the EF Core <see cref="RentalPipelineDbContext"/> (using the PostgreSQL provider),
    /// exposes it as <see cref="IUnitOfWork"/>, and registers all repository implementations.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<RentalPipelineDbContext>(options => options.UseNpgsql(connectionString));

        // The DbContext already implements IUnitOfWork; resolve both from the same scoped instance.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RentalPipelineDbContext>());

        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IRentalProposalRepository, RentalProposalRepository>();

        return services;
    }

    /// <summary>
    /// Registers Application-layer services and FluentValidation validators.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IRentalProposalService, RentalProposalService>();

        services.AddValidatorsFromAssemblyContaining<CreatePropertyValidator>();

        return services;
    }
}
