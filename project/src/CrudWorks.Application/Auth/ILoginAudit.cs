namespace CrudWorks.Application.Auth;

/// <summary>
/// Observability port for failed logins (SPEC-001 §8). Implementations must log only
/// the email domain — never the local part, the full email, or the password.
/// </summary>
/// <remarks>
/// Synchronous by design: the record must be emitted before the 401 response is
/// written, and the expected implementation is a non-blocking log write.
/// </remarks>
public interface ILoginAudit
{
    void FailedLogin(string emailDomain);
}
