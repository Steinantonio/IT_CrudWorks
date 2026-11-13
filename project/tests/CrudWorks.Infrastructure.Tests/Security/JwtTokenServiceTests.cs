using System.IdentityModel.Tokens.Jwt;
using CrudWorks.Application.Auth;
using CrudWorks.Domain.Users;
using CrudWorks.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;

namespace CrudWorks.Infrastructure.Tests.Security;

public sealed class JwtTokenServiceTests
{
    // 64 bytes of 'k', base64 encoded: fixed, non-secret key for tests only.
    private const string SigningKey =
        "a2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2traw==";

    private static readonly JwtOptions Options = new()
    {
        Issuer = "CrudWorks",
        Audience = "CrudWorks.Client",
        ExpiryMinutes = 60,
        SigningKey = SigningKey,
    };

    [Fact]
    public void Create_IssuesSignedTokenWithRequiredClaims()
    {
        var user = User.Create("demo@crudworks.local", "Demo User", "hash");
        var before = DateTime.UtcNow;

        var token = new JwtTokenService(Options).Create(user);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            token.AccessToken,
            CreateValidationParameters(SigningKey),
            out var validatedToken);

        Assert.Equal(user.Id.ToString(), principal.FindFirst(AuthClaimNames.Subject)?.Value);
        Assert.Equal("demo@crudworks.local", principal.FindFirst(AuthClaimNames.Email)?.Value);
        Assert.Equal("Demo User", principal.FindFirst(AuthClaimNames.Name)?.Value);
        Assert.False(string.IsNullOrWhiteSpace(principal.FindFirst(AuthClaimNames.JwtId)?.Value));
        Assert.Equal(DateTimeKind.Utc, token.ExpiresAtUtc.Kind);
        Assert.InRange(token.ExpiresAtUtc, before.AddMinutes(59), before.AddMinutes(61));
        Assert.Equal(token.ExpiresAtUtc, validatedToken.ValidTo.ToUniversalTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithAnotherKey_FailsValidation()
    {
        var user = User.Create("demo@crudworks.local", "Demo User", "hash");
        var token = new JwtTokenService(Options).Create(user);

        var otherKey = Convert.ToBase64String(new byte[64]);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(token.AccessToken, CreateValidationParameters(otherKey), out _));
    }

    private static TokenValidationParameters CreateValidationParameters(string signingKey) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Options.Issuer,
        ValidateAudience = true,
        ValidAudience = Options.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
        ClockSkew = TimeSpan.Zero,
    };
}
