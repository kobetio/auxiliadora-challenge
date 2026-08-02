using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RentalPipeline.Api.Middlewares;

/// <summary>
/// Centralized exception handler (Architecture.md's "Error Handling" section): logs every
/// unhandled exception and converts it into an RFC 7807 <c>ProblemDetails</c> response, so
/// controllers never need their own try/catch blocks. Two concurrency-related EF Core/PostgreSQL
/// exceptions are specifically mapped to <c>409 Conflict</c> (Architecture.md "Optimistic
/// Concurrency": "the API should translate [DbUpdateConcurrencyException] into 409 Conflict") since
/// they represent an expected, retryable race outcome rather than a genuine server error. Anything
/// else is truly unexpected and mapped to <c>500</c>.
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

    /// <summary>Invokes the rest of the pipeline, converting any unhandled exception into a ProblemDetails response.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // The RowVersion/xmin optimistic concurrency token didn't match on UPDATE: another
            // request modified (or deleted) the same row first.
            _logger.LogWarning(
                exception,
                "Concurrency conflict (stale RowVersion) while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Conflict",
                "The resource was modified by another request. Please retry.");
        }
        catch (DbUpdateException exception) when (IsSerializationFailure(exception))
        {
            // PostgreSQL's Serializable isolation level (Architecture.md Section 9) detected that
            // this transaction cannot be reconciled with another one that committed concurrently
            // (e.g. two requests racing to reserve the same Property) and aborted it.
            _logger.LogWarning(
                exception,
                "Serialization failure (concurrent transaction conflict) while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Conflict",
                "The request conflicted with another concurrent operation. Please retry.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while processing the request.");
        }
    }

    private static bool IsSerializationFailure(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure };

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
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
