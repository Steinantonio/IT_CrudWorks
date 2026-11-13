namespace CrudWorks.Application.Auth;

/// <summary>
/// Registration input. Fields are nullable because a JSON body may omit them; the
/// application layer is the authority that turns that into field-level errors.
/// </summary>
public sealed record RegisterRequest(string? Email, string? DisplayName, string? Password);
