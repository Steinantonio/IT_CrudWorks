namespace CrudWorks.Application.Common.Exceptions;

/// <summary>
/// Input failed a business validation rule. Carries field-level messages keyed by
/// the JSON field name so the API can emit a ProblemDetails <c>errors</c> map.
/// </summary>
public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
