using System.Security.Cryptography;
using CrudWorks.Application.Auth;
using CrudWorks.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace CrudWorks.Infrastructure.Security;

/// <summary>
/// Adapts ASP.NET Core's PBKDF2 <see cref="PasswordHasher{TUser}"/> to the
/// password-only Application abstraction. Rolling a custom hasher is forbidden.
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    // Verifying a random password against a real hash costs the same PBKDF2 work as
    // a normal failed verification. It is computed once, lazily, and only hashed on
    // the first unknown-email login, so the Process never pays for it at startup.
    private static readonly Lazy<string> DummyHash = new(() =>
        new PasswordHasher<User>().HashPassword(null!, GenerateRandomPassword()));

    // PasswordHasher<TUser> accepts a user for extensibility; the default
    // implementation never reads it, and the Application contract is password-only.
    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string? hash)
    {
        // A missing stored hash means the account does not exist (or is corrupted).
        // Verifying against the dummy hash instead of returning immediately keeps the
        // unknown-email path constant-time with the wrong-password path, so response
        // timing cannot be used to enumerate accounts.
        var effectiveHash = string.IsNullOrEmpty(hash) ? DummyHash.Value : hash;

        try
        {
            var result = _hasher.VerifyHashedPassword(null!, effectiveHash, password);
            return result is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // A malformed stored hash must behave like a failed verification, not a 500.
            return false;
        }
    }

    private static string GenerateRandomPassword()
    {
        // 16 random alphanumeric characters; the value is discarded — only its hash
        // is kept, and it is never compared against a real password by design.
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = RandomNumberGenerator.GetBytes(16);
        return new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
    }
}
