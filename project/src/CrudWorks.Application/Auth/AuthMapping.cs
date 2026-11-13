using CrudWorks.Domain.Users;

namespace CrudWorks.Application.Auth;

internal static class AuthMapping
{
    public static AuthUserResponse ToAuthUserResponse(this User user) =>
        new(user.Id, user.Email, user.DisplayName, user.CreatedAtUtc);

    public static UserSummary ToUserSummary(this User user) =>
        new(user.Id, user.Email, user.DisplayName);
}
