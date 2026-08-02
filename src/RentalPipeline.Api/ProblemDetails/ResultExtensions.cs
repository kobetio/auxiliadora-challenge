using FluentResults;
using Microsoft.AspNetCore.Mvc;
using RentalPipeline.Application.Errors;

namespace RentalPipeline.Api.ProblemDetails;

/// <summary>
/// Translates Application-layer <see cref="Result"/>/<see cref="Result{T}"/> outcomes into HTTP
/// responses, generating RFC 7807 <c>ProblemDetails</c> for failures. Controllers use these
/// extensions instead of hand-rolling status-code logic or try/catch blocks (per Architecture.md's
/// "Controllers should never contain try/catch blocks" guideline).
/// </summary>
public static class ResultExtensions
{
    /// <summary>Maps a successful result to <c>200 OK</c>, or a failure to its ProblemDetails response.</summary>
    public static ActionResult<T> ToOkResult<T>(this ControllerBase controller, Result<T> result) =>
        result.IsSuccess ? controller.Ok(result.Value) : controller.ToProblem(result.Errors);

    /// <summary>Maps a successful result to <c>204 No Content</c>, or a failure to its ProblemDetails response.</summary>
    public static ActionResult ToNoContentResult(this ControllerBase controller, Result result) =>
        result.IsSuccess ? controller.NoContent() : controller.ToProblem(result.Errors);

    /// <summary>
    /// Maps a successful result to <c>201 Created</c> (with a <c>Location</c> header built from
    /// <paramref name="actionName"/> and <paramref name="routeValues"/>), or a failure to its
    /// ProblemDetails response.
    /// </summary>
    public static ActionResult<T> ToCreatedResult<T>(
        this ControllerBase controller,
        Result<T> result,
        string actionName,
        Func<T, object> routeValues) =>
        result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues(result.Value!), result.Value)
            : controller.ToProblem(result.Errors);

    private static ActionResult ToProblem(this ControllerBase controller, IEnumerable<IError> errors)
    {
        var error = errors.First();
        var (statusCode, title) = error switch
        {
            NotFoundError => (StatusCodes.Status404NotFound, "Not Found"),
            ConflictError => (StatusCodes.Status409Conflict, "Conflict"),
            BusinessRuleViolationError => (StatusCodes.Status400BadRequest, "Business Rule Violation"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request"),
        };

        return controller.Problem(
            detail: error.Message,
            instance: controller.HttpContext.Request.Path,
            statusCode: statusCode,
            title: title);
    }
}
