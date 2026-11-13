namespace CrudWorks.Application.Auth;

public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);
