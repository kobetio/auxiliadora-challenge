using FluentResults;

namespace RentalPipeline.Application.Errors;

/// <summary>
/// Signals a violation of a business rule that isn't a simple "not found" or state conflict
/// (e.g. an invalid proposal status transition — Rule 4). Controllers translate this into
/// <c>400 Bad Request</c>, per the endpoint-specific responses documented for
/// <c>PATCH /proposals/{id}/status</c>.
/// </summary>
public sealed class BusinessRuleViolationError : Error
{
    public BusinessRuleViolationError(string message) : base(message)
    {
    }
}
