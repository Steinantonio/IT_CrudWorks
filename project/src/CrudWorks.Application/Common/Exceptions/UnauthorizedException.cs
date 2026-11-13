namespace CrudWorks.Application.Common.Exceptions;

/// <summary>The caller is not authenticated, or the supplied credentials are invalid.</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
