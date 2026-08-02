using FluentResults;

namespace RentalPipeline.Application.Errors;

/// <summary>
/// Signals that a requested resource does not exist. Controllers translate this into <c>404 Not Found</c>.
/// </summary>
public sealed class NotFoundError : Error
{
    public NotFoundError(string message) : base(message)
    {
    }
}
