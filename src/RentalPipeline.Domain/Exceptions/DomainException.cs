namespace RentalPipeline.Domain.Exceptions;

/// <summary>
/// Represents a violation of a domain invariant.
/// </summary>
/// <remarks>
/// This exception must only be thrown for states that should never occur if the
/// Application layer performed its expected business validation beforehand
/// (defense in depth). Expected business failures (e.g. "property unavailable")
/// must be communicated to the caller through <c>Result&lt;T&gt;</c> at the
/// Application layer, not through exceptions.
/// </remarks>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
