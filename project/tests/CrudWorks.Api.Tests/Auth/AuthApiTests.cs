using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CrudWorks.Application.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CrudWorks.Api.Tests.Auth;

public sealed class AuthApiTests(CrudWorksApiFactory factory) : IClassFixture<CrudWorksApiFactory>
{
    [Fact]
    public async Task Register_WithValidPayload_Returns201WithLocationAndUser()
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "New User", "Secret123"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/api/auth/me", response.Headers.Location.OriginalString);

        var body = await response.Content.ReadFromJsonAsync<AuthUserResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(email, body.Email);
        Assert.Equal("New User", body.DisplayName);
        Assert.Equal(DateTimeKind.Utc, body.CreatedAtUtc.Kind);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Secret123", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithSeededDemoEmailInOtherCaseAndSpaces_Returns409()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("  DEMO@CrudWorks.local  ", "Duplicate User", "Secret123"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    }

    [Fact]
    public async Task Register_WithInvalidPayload_Returns400WithFieldLevelErrors()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("not-an-email", "D", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.True(problem.Errors.ContainsKey("email"));
        Assert.True(problem.Errors.ContainsKey("displayName"));
        Assert.True(problem.Errors.ContainsKey("password"));
        Assert.Equal("Email must be a valid address.", problem.Errors["email"][0]);
        Assert.Equal("Display name must be between 2 and 100 characters.", problem.Errors["displayName"][0]);
        Assert.Equal(
            "Password must be 8–128 characters and contain a letter and a digit.",
            problem.Errors["password"][0]);
    }

    [Fact]
    public async Task Register_WithOmittedFields_Returns400ForEachField()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("email"));
        Assert.True(problem.Errors.ContainsKey("displayName"));
        Assert.True(problem.Errors.ContainsKey("password"));
    }

    [Fact]
    public async Task Register_WithMalformedJson_Returns400ProblemDetails()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent("{ \"email\": ", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenThatAuthenticatesMe()
    {
        using var client = factory.CreateClient();
        var email = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "Secret123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.True(body.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(email, body.User.Email);
        Assert.Equal("Test User", body.User.DisplayName);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var me = await client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var meBody = await me.Content.ReadFromJsonAsync<AuthUserResponse>();
        Assert.NotNull(meBody);
        Assert.Equal(body.User.Id, meBody.Id);
        Assert.Equal(email, meBody.Email);
    }

    [Fact]
    public async Task Login_WithWrongPasswordAndUnknownEmail_ReturnsTheSame401()
    {
        using var client = factory.CreateClient();
        var email = await RegisterAsync(client);

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "Wrong123"));
        var unknownEmail = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(UniqueEmail(), "Secret123"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal("application/problem+json", wrongPassword.Content.Headers.ContentType?.MediaType);

        var wrongProblem = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>();
        var unknownProblem = await unknownEmail.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(wrongProblem);
        Assert.NotNull(unknownProblem);
        Assert.Equal("Invalid email or password", wrongProblem.Title);
        Assert.Equal(wrongProblem.Title, unknownProblem.Title);
        Assert.Equal(wrongProblem.Status, unknownProblem.Status);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401WithAuthenticationRequired()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Authentication required", problem.Title);
    }

    [Fact]
    public async Task Me_WithMalformedToken_Returns401()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithExpiredToken_Returns401()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateExpiredToken());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithAValidTokenWhoseSubjectIsNotAGuid_Returns401()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateTokenWithSubject("not-a-guid"));

        var response = await client.SendAsync(request);

        // The token itself is valid, but the owner cannot be resolved from it, so the
        // caller is not authenticated (AGENTS.md §4: the owner always comes from the sub).
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Authentication required", problem.Title);
    }

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        var email = UniqueEmail();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Test User", "Secret123"));
        response.EnsureSuccessStatusCode();
        return email;
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static string CreateTokenWithSubject(string subject)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(CrudWorksApiFactory.TestSigningKey));
        var token = new JwtSecurityToken(
            issuer: "CrudWorks",
            audience: "CrudWorks.Client",
            claims:
            [
                new Claim(AuthClaimNames.Subject, subject),
                new Claim(AuthClaimNames.Email, "subject@example.com"),
                new Claim(AuthClaimNames.Name, "Subject User"),
                new Claim(AuthClaimNames.JwtId, Guid.NewGuid().ToString()),
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateExpiredToken()
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(CrudWorksApiFactory.TestSigningKey));
        var token = new JwtSecurityToken(
            issuer: "CrudWorks",
            audience: "CrudWorks.Client",
            claims:
            [
                new Claim(AuthClaimNames.Subject, Guid.NewGuid().ToString()),
                new Claim(AuthClaimNames.Email, "expired@example.com"),
                new Claim(AuthClaimNames.Name, "Expired User"),
                new Claim(AuthClaimNames.JwtId, Guid.NewGuid().ToString()),
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
