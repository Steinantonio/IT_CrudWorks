using CrudWorks.Application.Common.Exceptions;
using CrudWorks.Application.Users;
using CrudWorks.Domain.Users;

namespace CrudWorks.Application.Auth;

public sealed class AuthService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ILoginAudit loginAudit) : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password";
    private const string EmailConflictMessage = "Email already registered.";

    public async Task<AuthUserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ThrowIfInvalid(AuthValidation.ValidateRegister(request));

        var normalizedEmail = User.NormalizeEmail(request.Email);
        if (await userRepository.FindByEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            throw new ConflictException(EmailConflictMessage);
        }

        var user = User.Create(
            normalizedEmail,
            request.DisplayName!,
            passwordHasher.Hash(request.Password!));

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToAuthUserResponse();
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ThrowIfInvalid(AuthValidation.ValidateLogin(request));

        var normalizedEmail = User.NormalizeEmail(request.Email);
        var user = await userRepository.FindByEmailAsync(normalizedEmail, cancellationToken);

        // Verify always runs — with a null hash when the email is unknown — so both
        // failure paths do the same PBKDF2 work and response timing cannot be used
        // to enumerate accounts. The adapter verifies against a dummy hash in that case.
        var verified = passwordHasher.Verify(request.Password!, user?.PasswordHash);
        if (!verified || user is null)
        {
            // SPEC-001 §8: failed logins are audited with the email domain only;
            // never the local part, the full email, or the password.
            loginAudit.FailedLogin(GetEmailDomain(normalizedEmail));

            // The same message and status are returned for an unknown email and a wrong
            // password so the response cannot be used to enumerate accounts.
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var token = tokenService.Create(user);
        return new LoginResponse(token.AccessToken, token.ExpiresAtUtc, user.ToUserSummary());
    }

    private static string GetEmailDomain(string normalizedEmail)
    {
        var separatorIndex = normalizedEmail.LastIndexOf('@');
        return separatorIndex < 0 ? string.Empty : normalizedEmail[(separatorIndex + 1)..];
    }

    public async Task<AuthUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("Authentication required");

        return user.ToAuthUserResponse();
    }

    private static void ThrowIfInvalid(IReadOnlyDictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
