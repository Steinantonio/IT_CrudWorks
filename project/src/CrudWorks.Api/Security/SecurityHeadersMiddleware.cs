namespace CrudWorks.Api.Security;

/// <summary>
/// Adds the security response headers approved by SPEC-007. The values live here
/// so every environment shares one policy; only HSTS varies by environment.
/// Headers are applied when the response starts, so a response rewritten by the
/// exception handler or the status-code pages still carries them.
/// </summary>
internal sealed class SecurityHeadersMiddleware
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; "
        + "connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    public const string Hsts = "max-age=31536000; includeSubDomains";

    private readonly RequestDelegate _next;
    private readonly bool _includeHsts;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _includeHsts = !environment.IsDevelopment();
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(ApplyHeaders, context);
        return _next(context);
    }

    private Task ApplyHeaders(object state)
    {
        var headers = ((HttpContext)state).Response.Headers;

        headers["Content-Security-Policy"] = ContentSecurityPolicy;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Frame-Options"] = "DENY";

        if (_includeHsts)
        {
            headers["Strict-Transport-Security"] = Hsts;
        }

        return Task.CompletedTask;
    }
}

internal static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
