using FluentResults;

namespace RentalPipeline.Application.Errors;

/// <summary>
/// Signals that the requested operation conflicts with the current state of a resource
/// (e.g. the property is already in negotiation — Rule 2). Controllers translate this into
/// <c>409 Conflict</c>.
/// </summary>
public sealed class ConflictError : Error
{
    public ConflictError(string message) : base(message)
    {
    }
}
