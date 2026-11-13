using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CrudWorks.Application.Auth;
using CrudWorks.Domain.Users;
using Microsoft.IdentityModel.Tokens;

namespace CrudWorks.Infrastructure.Security;

public sealed class JwtTokenService(JwtOptions options) : ITokenService
{
    public TokenResult Create(User user)
    {
        var signingKey = options.SigningKey
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(options.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(AuthClaimNames.Subject, user.Id.ToString("D", CultureInfo.InvariantCulture)),
            new Claim(AuthClaimNames.Email, user.Email),
            new Claim(AuthClaimNames.Name, user.DisplayName),
            new Claim(AuthClaimNames.JwtId, Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture)),
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
