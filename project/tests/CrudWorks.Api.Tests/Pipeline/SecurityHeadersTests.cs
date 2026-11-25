using System.Net;
using System.Net.Http.Json;
using CrudWorks.Application.Auth;

namespace CrudWorks.Api.Tests.Pipeline;

/// <summary>
/// SPEC-007: the API owns the security response headers because it serves the SPA
/// in deployment. Values are asserted exactly so the policy cannot drift between
/// environments; only HSTS varies (absent in Development, present elsewhere).
/// </summary>
public sealed class SecurityHeadersTests(
    CrudWorksApiFactory developmentFactory,
    NonDevelopmentApiFactory productionFactory,
    SpaServingApiFactory spaFactory,
    FailingAuthServiceApiFactory failingAuthServiceFactory)
    : IClassFixture<CrudWorksApiFactory>,
      IClassFixture<NonDevelopmentApiFactory>,
      IClassFixture<SpaServingApiFactory>,
      IClassFixture<FailingAuthServiceApiFactory>
{
    private const string ExpectedCsp =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; "
        + "connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    private const string ExpectedHsts = "max-age=31536000; includeSubDomains";

    [Fact]
    public async Task Development_HealthResponse_CarriesBaselineHeadersWithoutHsts()
    {
        using var client = developmentFactory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertBaselineHeaders(response);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Production_HealthResponse_CarriesBaselineHeadersAndHsts()
    {
        using var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertBaselineHeaders(response);
        AssertHsts(response);
    }

    [Fact]
    public async Task UnauthorizedResponse_CarriesBaselineHeaders()
    {
        using var client = developmentFactory.CreateClient();

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertBaselineHeaders(response);
    }

    [Fact]
    public async Task UnknownRoute_CarriesBaselineHeaders()
    {
        using var client = developmentFactory.CreateClient();

        var response = await client.GetAsync("/definitely-not-a-page");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertBaselineHeaders(response);
    }

    [Fact]
    public async Task SpaDocumentAndDeepLink_CarryBaselineHeadersAndHsts()
    {
        using var client = spaFactory.CreateClient();

        foreach (var path in new[] { "/", "/tasks/123" })
        {
            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            AssertBaselineHeaders(response);
            AssertHsts(response);
        }
    }

    [Fact]
    public async Task UnhandledException_CarriesBaselineHeadersOnTheProblemJsonResponse()
    {
        using var client = failingAuthServiceFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("demo@crudworks.local", "Demo123!"));

        // The exception handler clears the response before writing problem+json; the
        // headers must be re-applied when that rewritten response starts.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        AssertBaselineHeaders(response);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task ContentSecurityPolicy_IsStrictAndExact()
    {
        using var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/api/health");

        var policy = response.Headers.GetValues("Content-Security-Policy").Single();

        Assert.Equal(ExpectedCsp, policy);
        Assert.DoesNotContain("unsafe-inline", policy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unsafe-eval", policy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("*", policy, StringComparison.Ordinal);
    }

    private static void AssertBaselineHeaders(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(
            "strict-origin-when-cross-origin",
            response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal(ExpectedCsp, response.Headers.GetValues("Content-Security-Policy").Single());
    }

    private static void AssertHsts(HttpResponseMessage response)
    {
        var hsts = response.Headers.GetValues("Strict-Transport-Security").Single();

        Assert.Equal(ExpectedHsts, hsts);
        Assert.DoesNotContain("preload", hsts, StringComparison.OrdinalIgnoreCase);
    }
}
