using System.Net.Mail;
using CrudWorks.Domain.Users;

namespace CrudWorks.Application.Auth;

/// <summary>
/// Business validation for the auth use cases. Messages match the API contract in
/// ADR-0004 so the client can display them directly.
/// </summary>
internal static class AuthValidation
{
    public const string EmailMessage = "Email must be a valid address.";
    public const string DisplayNameMessage = "Display name must be between 2 and 100 characters.";
    public const string PasswordMessage = "Password must be 8–128 characters and contain a letter and a digit.";
    public const string PasswordRequiredMessage = "Password is required.";

    private const int PasswordMinLength = 8;
    private const int PasswordMaxLength = 128;

    public static Dictionary<string, string[]> ValidateRegister(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (!IsValidEmail(request.Email))
        {
            errors["email"] = [EmailMessage];
        }

        if (!IsValidDisplayName(request.DisplayName))
        {
            errors["displayName"] = [DisplayNameMessage];
        }

        if (!IsStrongPassword(request.Password))
        {
            errors["password"] = [PasswordMessage];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateLogin(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (!IsValidEmail(request.Email))
        {
            errors["email"] = [EmailMessage];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = [PasswordRequiredMessage];
        }

        return errors;
    }

    private static bool IsValidEmail(string? email)
    {
        var trimmed = email?.Trim();
        return !string.IsNullOrWhiteSpace(trimmed)
            && trimmed.Length <= User.EmailMaxLength
            && MailAddress.TryCreate(trimmed, out var parsed)
            && string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidDisplayName(string? displayName)
    {
        var trimmed = displayName?.Trim();
        return trimmed is not null
            && trimmed.Length >= User.DisplayNameMinLength
            && trimmed.Length <= User.DisplayNameMaxLength;
    }

    private static bool IsStrongPassword(string? password) =>
        password is not null
        && password.Length is >= PasswordMinLength and <= PasswordMaxLength
        && password.Any(char.IsLetter)
        && password.Any(char.IsDigit);
}
