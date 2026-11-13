namespace CrudWorks.Application.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserSummary User);
