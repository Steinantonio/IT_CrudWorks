namespace CrudWorks.Application.Auth;

/// <summary>Claim names shared by the token issuer (Infrastructure) and the reader (Api).</summary>
public static class AuthClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string JwtId = "jti";
}
