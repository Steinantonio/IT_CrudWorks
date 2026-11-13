namespace CrudWorks.Application.Common.Exceptions;

/// <summary>The request conflicts with persisted state, for example a duplicate email.</summary>
public sealed class ConflictException(string message) : Exception(message);
