using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RentalPipeline.Api.Extensions;
using RentalPipeline.Api.Filters;
using RentalPipeline.Api.Middlewares;
using RentalPipeline.Infrastructure.Persistence;

// Force English validation messages regardless of the host machine's OS locale, so API responses
// are consistent across environments instead of depending on where the process happens to run.
ValidatorOptions.Global.LanguageManager.Enabled = false;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Rental Pipeline API",
        Version = "v1",
        Description = "Manages the residential rental proposal pipeline, from proposal creation to active contract.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationServices();

var app = builder.Build();

// Apply pending EF Core migrations automatically on startup, so `docker compose up` (or any other
// deployment) brings up a fully-migrated database with zero manual steps — no `dotnet-ef` tool or
// separate `dotnet ef database update` command required on the host. Acceptable for this project's
// single-instance deployment model; see ARCHITECTURE_DECISIONS.md for the trade-offs of this choice
// versus a separate migration step for higher-scale/multi-instance deployments.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RentalPipelineDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.

// Must run first so it can catch exceptions raised by any later middleware/controller.
app.UseExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI();

// The Docker image (see Dockerfile/docker-compose.yml) only exposes plain HTTP on port 8080 with no
// HTTPS binding at all, so redirecting would be pointless and would just log a "Failed to determine
// the https port for redirect" warning on every single request. `DOTNET_RUNNING_IN_CONTAINER` is set
// automatically by Microsoft's .NET container base images, so this only skips the redirect there;
// running the API directly on the host via `dotnet run` (with Kestrel's HTTPS dev profile) keeps it.
if (!builder.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Exposes the top-level-statement-generated <c>Program</c> class so integration tests can boot the
/// API in-memory via <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
