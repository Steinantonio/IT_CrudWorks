using CrudWorks.Application.Auth;

namespace CrudWorks.Api.Security;

/// <summary>
/// Logs failed logins (SPEC-001 §8). Only the email domain is recorded — never the
/// local part, the full email, or the password — so the log cannot become an
/// account roster or a credential leak.
/// </summary>
public sealed class LoginAudit(ILogger<LoginAudit> logger) : ILoginAudit
{
    public void FailedLogin(string emailDomain) =>
        logger.LogWarning("Failed login attempt for email domain {EmailDomain}", emailDomain);
}
