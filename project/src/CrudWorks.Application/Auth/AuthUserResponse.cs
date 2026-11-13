namespace CrudWorks.Application.Auth;

public sealed record AuthUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    DateTime CreatedAtUtc);
