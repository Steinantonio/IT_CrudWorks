using CrudWorks.Domain.Common;

namespace CrudWorks.Domain.Users;

/// <summary>
/// A registered user. The password is only ever stored as a hash produced by the
/// password hasher; the plaintext password never enters the domain model.
/// </summary>
public sealed class User
{
    public const int EmailMaxLength = 256;
    public const int DisplayNameMinLength = 2;
    public const int DisplayNameMaxLength = 100;

    private User()
    {
    }

    private User(Guid id, string email, string displayName, string passwordHash, DateTime createdAtUtc)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    /// <summary>Stored normalized: trimmed and lowercased.</summary>
    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trims and lowercases an email so lookups and storage agree.</summary>
    public static string NormalizeEmail(string? email) =>
        email is null ? string.Empty : email.Trim().ToLowerInvariant();

    public static User Create(string email, string displayName, string passwordHash)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail.Length == 0)
        {
            throw new DomainException("Email is required.");
        }

        if (normalizedEmail.Length > EmailMaxLength)
        {
            throw new DomainException($"Email must be at most {EmailMaxLength} characters.");
        }

        var trimmedDisplayName = displayName?.Trim() ?? string.Empty;
        if (trimmedDisplayName.Length < DisplayNameMinLength || trimmedDisplayName.Length > DisplayNameMaxLength)
        {
            throw new DomainException(
                $"Display name must be between {DisplayNameMinLength} and {DisplayNameMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        return new User(
            Guid.NewGuid(),
            normalizedEmail,
            trimmedDisplayName,
            passwordHash,
            DateTime.UtcNow);
    }
}
