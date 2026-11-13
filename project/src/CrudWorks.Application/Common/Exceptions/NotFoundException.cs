namespace CrudWorks.Application.Common.Exceptions;

/// <summary>
/// The resource does not exist for the caller. Ownership failures use this type so
/// the API can answer 404 instead of confirming that another user's resource exists.
/// </summary>
public sealed class NotFoundException(string message) : Exception(message);
