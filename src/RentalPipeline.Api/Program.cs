using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.OpenApi;
using RentalPipeline.Api.Extensions;
using RentalPipeline.Api.Filters;
using RentalPipeline.Api.Middlewares;

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

// Configure the HTTP request pipeline.

// Must run first so it can catch exceptions raised by any later middleware/controller.
app.UseExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Exposes the top-level-statement-generated <c>Program</c> class so integration tests can boot the
/// API in-memory via <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
