using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using CrudWorks.Application.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace CrudWorks.Api.Tests.Pipeline;

/// <summary>
/// Covers the signing-key resolution rule: configuration wins, Development falls
/// back to an ephemeral key with a warning, and any other environment fails fast.
/// </summary>
public sealed class JwtSigningKeyTests : IClassFixture<DevelopmentWithoutSigningKeyApiFactory>
{
    private readonly DevelopmentWithoutSigningKeyApiFactory _factory;

    public JwtSigningKeyTests(DevelopmentWithoutSigningKeyApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Development_WithoutAConfiguredSigningKey_StartsAndSignsWithAnEphemeralKey()
    {
        using var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Key User", "Secret123"));
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "Secret123"));
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);

        // The token the API issued is accepted by the ephemeral key.
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var me = await client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        // A token signed with the configured test key is rejected, which proves the
        // ephemeral key - and not the configured one - is the key in use.
        using var foreignRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        foreignRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(CrudWorksApiFactory.TestSigningKey));
        var foreign = await client.SendAsync(foreignRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, foreign.StatusCode);
    }

    [Fact]
    public void NonDevelopment_WithoutAConfiguredSigningKey_FailsTheStartup()
    {
        using var factory = new ProductionWithoutSigningKeyApiFactory();

        // The host must not come up without a signing key outside Development.
        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(
            "Jwt:SigningKey is required outside Development",
            exception.ToString(),
            StringComparison.Ordinal);
    }

    [Theory]
    // Not base64 at all.
    [InlineData("not-base64!", "Jwt:SigningKey must be base64 encoded.")]
    // Valid base64, but it decodes to 16 bytes - below the 32-byte minimum.
    [InlineData("MDEyMzQ1Njc4OWFiY2RlZg==", "Jwt:SigningKey must decode to at least 32 bytes.")]
    public void AConfiguredSigningKeyThatIsInvalid_FailsTheStartup(string signingKey, string expectedMessage)
    {
        // The key is validated in every environment: a broken secret is a startup
        // failure, not a runtime surprise.
        using var factory = new InvalidSigningKeyApiFactory(signingKey, Environments.Development);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(expectedMessage, exception.ToString(), StringComparison.Ordinal);
    }

    private static string CreateToken(string signingKey)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(signingKey));
        var token = new JwtSecurityToken(
            issuer: "CrudWorks",
            audience: "CrudWorks.Client",
            claims:
            [
                new Claim(AuthClaimNames.Subject, Guid.NewGuid().ToString()),
                new Claim(AuthClaimNames.Email, "key@example.com"),
                new Claim(AuthClaimNames.Name, "Key User"),
                new Claim(AuthClaimNames.JwtId, Guid.NewGuid().ToString()),
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Development host with no <c>Jwt:SigningKey</c> in configuration: the API must
/// still start, on an ephemeral per-process key.
/// </summary>
public sealed class DevelopmentWithoutSigningKeyApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"crudworks-api-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // No Jwt:SigningKey on purpose.
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_databasePath};Pooling=False",
            });
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

/// <summary>
/// Non-Development host with no <c>Jwt:SigningKey</c> in configuration: the API must
/// refuse to start.
/// </summary>
public sealed class ProductionWithoutSigningKeyApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // No Jwt:SigningKey on purpose.
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    $"Data Source={Path.Combine(Path.GetTempPath(), $"crudworks-api-tests-{Guid.NewGuid():N}.db")};Pooling=False",
            });
        });
    }
}

/// <summary>Host with a configured but invalid <c>Jwt:SigningKey</c>.</summary>
public sealed class InvalidSigningKeyApiFactory(string signingKey, string environment)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = signingKey,
                ["ConnectionStrings:Default"] =
                    $"Data Source={Path.Combine(Path.GetTempPath(), $"crudworks-api-tests-{Guid.NewGuid():N}.db")};Pooling=False",
            });
        });
    }
}
