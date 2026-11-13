using System.Net;
using System.Net.Http.Json;
using CrudWorks.Application.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CrudWorks.Api.Tests.Pipeline;

/// <summary>
/// Covers the API pipeline seams that the endpoint tests do not reach: the RFC 7807
/// error contract, the CORS policy, and the OpenAPI document.
/// </summary>
public sealed class ApiPipelineTests : IClassFixture<CrudWorksApiFactory>, IClassFixture<FailingAuthServiceApiFactory>
{
    private const string AllowedOrigin = "http://localhost:5173";

    private readonly CrudWorksApiFactory _factory;
    private readonly FailingAuthServiceApiFactory _failingAuthServiceFactory;

    public ApiPipelineTests(
        CrudWorksApiFactory factory,
        FailingAuthServiceApiFactory failingAuthServiceFactory)
    {
        _factory = factory;
        _failingAuthServiceFactory = failingAuthServiceFactory;
    }

    [Fact]
    public async Task ProblemDetails_ForAValidationFailure_CarriesTheContractTypeStatusAndTraceId()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("not-an-email", "D", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("One or more validation errors occurred.", problem.Title);
        // ADR-0004 §1: the RFC 7807 type points at the status section.
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", problem.Type);
        // The trace id lets an operator correlate a report with the server log.
        var traceId = problem.Extensions["traceId"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(traceId));
        // Field-level errors travel in the "errors" member.
        Assert.True(problem.Errors.ContainsKey("email"));
    }

    [Fact]
    public async Task ProblemDetails_ForAnUnexpectedFailure_Returns500WithAGenericTitleAndNoInternalDetail()
    {
        using var client = _failingAuthServiceFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("demo@crudworks.local", "Demo123!"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var raw = await response.Content.ReadAsStringAsync();
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", problem.Type);
        // AGENTS.md §4: no stack traces, SQL, or internal identifiers reach the client.
        Assert.DoesNotContain("Sensitive internal detail", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cors_PreflightFromTheConfiguredOrigin_ReturnsTheOriginMethodsAndHeaders()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/tasks");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type, authorization");

        var response = await client.SendAsync(request);

        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        var methods = response.Headers.GetValues("Access-Control-Allow-Methods").Single();
        Assert.Contains("POST", methods, StringComparison.Ordinal);
        var headers = response.Headers.GetValues("Access-Control-Allow-Headers").Single();
        Assert.Contains("Content-Type", headers, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Authorization", headers, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cors_PreflightFromAnUnknownOrigin_ReturnsNoAllowOriginHeader()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/tasks");
        request.Headers.Add("Origin", "http://evil.example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        // Only the configured frontend origin may read the response.
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Cors_ActualRequestFromTheConfiguredOrigin_ReturnsTheAllowOriginHeader()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task ProblemDetails_ForAnUnknownRoute_Returns404AsProblemJson()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/definitely-not-a-route");

        // ADR-0004 §1: routing-level 404s use the same RFC 7807 contract as the
        // controller and exception-handler errors, not the default text/plain body.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task OpenApiDocument_InDevelopment_IsServed()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"openapi\"", raw, StringComparison.Ordinal);
    }
}

/// <summary>
/// Hosts the real API pipeline with an application service that always fails, so the
/// "unexpected error" branch of the exception handler can be exercised.
/// </summary>
public sealed class FailingAuthServiceApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"crudworks-api-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = CrudWorksApiFactory.TestSigningKey,
                ["ConnectionStrings:Default"] = $"Data Source={_databasePath};Pooling=False",
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddScoped<IAuthService, ThrowingAuthService>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
    }
}

/// <summary>An auth service that fails the way an unexpected infrastructure fault would.</summary>
public sealed class ThrowingAuthService : IAuthService
{
    private const string InternalDetail = "Sensitive internal detail: the connection string is secret.";

    public Task<AuthUserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(InternalDetail);

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(InternalDetail);

    public Task<AuthUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(InternalDetail);
}
