using CrudWorks.Application.Auth;
using CrudWorks.Application.Common;

namespace CrudWorks.Api.Security;

/// <summary>Reads the caller from the validated access token, never from the request body.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirst(AuthClaimNames.Subject)?.Value;
            return Guid.TryParse(subject, out var userId) ? userId : null;
        }
    }
}
