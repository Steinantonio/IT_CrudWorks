namespace CrudWorks.Application.Auth;

public interface IAuthService
{
    Task<AuthUserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
