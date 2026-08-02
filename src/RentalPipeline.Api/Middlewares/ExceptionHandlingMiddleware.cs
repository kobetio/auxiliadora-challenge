namespace RentalPipeline.Api.Middlewares;

/// <summary>
/// Centralized exception handler (Architecture.md's "Error Handling" section): logs every
/// unhandled exception at Error level and converts it into an RFC 7807 <c>ProblemDetails</c>
/// <c>500</c> response, so controllers never need their own try/catch blocks. Any exception that
/// reaches here is by definition unexpected — expected business failures are always represented as
/// <c>Result&lt;T&gt;</c> failures and never throw.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>Creates the middleware with the next delegate in the pipeline and a logger.</summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Invokes the rest of the pipeline, converting any unhandled exception into a ProblemDetails 500.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing the request.",
                Instance = context.Request.Path,
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}

/// <summary>Registration helper for <see cref="ExceptionHandlingMiddleware"/>.</summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>Registers <see cref="ExceptionHandlingMiddleware"/>. Must run first in the pipeline
    /// so it can catch exceptions from every other middleware/controller.</summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
