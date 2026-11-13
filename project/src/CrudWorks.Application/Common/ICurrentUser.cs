namespace CrudWorks.Application.Common;

/// <summary>
/// The identity of the authenticated caller, resolved from the access token by the
/// API host. Application code never reads the user id from the request body.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}
