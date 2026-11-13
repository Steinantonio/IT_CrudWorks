namespace CrudWorks.Application.Auth;

/// <summary>
/// JWT settings bound from the <c>Jwt</c> configuration section. The signing key is
/// supplied by configuration, environment, or user-secrets; it is never committed.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CrudWorks";

    public string Audience { get; set; } = "CrudWorks.Client";

    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>Base64-encoded HMAC key of at least 32 bytes. Absent in committed settings.</summary>
    public string? SigningKey { get; set; }
}
