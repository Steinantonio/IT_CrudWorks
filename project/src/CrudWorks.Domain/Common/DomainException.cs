namespace CrudWorks.Domain.Common;

/// <summary>
/// Raised when a domain invariant is violated. The message must be safe to show
/// to a client; it must not contain internal state or secrets.
/// </summary>
public sealed class DomainException(string message) : Exception(message)
{
}
