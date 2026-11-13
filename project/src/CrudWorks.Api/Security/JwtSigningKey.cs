using System.Security.Cryptography;

namespace CrudWorks.Api.Security;

/// <summary>
/// Resolves the HMAC key for token signing. Configuration wins; Development falls
/// back to an ephemeral per-process key so the demo runs without a committed secret.
/// </summary>
internal static class JwtSigningKey
{
    private const int MinimumKeyBytes = 32;
    private const int EphemeralKeyBytes = 64;

    private static string? _ephemeralKey;

    public static (string Key, bool IsEphemeral) Resolve(string? configuredKey, IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            return (Validate(configuredKey), false);
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is required outside Development. Provide it through user-secrets or the Jwt__SigningKey environment variable.");
        }

        _ephemeralKey ??= Convert.ToBase64String(RandomNumberGenerator.GetBytes(EphemeralKeyBytes));
        return (_ephemeralKey, true);
    }

    private static string Validate(string configuredKey)
    {
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be base64 encoded.", exception);
        }

        if (keyBytes.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must decode to at least {MinimumKeyBytes} bytes.");
        }

        return configuredKey;
    }
}
