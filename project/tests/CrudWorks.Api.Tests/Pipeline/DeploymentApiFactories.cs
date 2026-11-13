using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CrudWorks.Api.Tests.Pipeline;

/// <summary>
/// Hosts the real API pipeline in the Production environment — the way the demo
/// deployment runs it — with an isolated temp database and the fixed test signing key.
/// Deployment behavior is opt-in through configuration, so derived factories add the
/// deployment flags (Database:RunMigrationsAndSeed, Spa:Serving).
/// </summary>
public class NonDevelopmentApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"crudworks-api-tests-{Guid.NewGuid():N}.db");

    public string DatabasePath => _databasePath;

    /// <summary>Deployment flags the derived host opts into; none by default.</summary>
    protected virtual IReadOnlyDictionary<string, string?> DeploymentConfiguration { get; } =
        new Dictionary<string, string?>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = CrudWorksApiFactory.TestSigningKey,
                ["ConnectionStrings:Default"] = $"Data Source={_databasePath};Pooling=False",
            });
            configuration.AddInMemoryCollection(DeploymentConfiguration);
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
/// Hosts the API in Production with Spa:Serving=true and a real wwwroot/index.html
/// fixture, mirroring the deployment where the Dockerfile copies the built React app
/// into wwwroot. The factory creates the fixture itself so the tests never depend on
/// files left in the repository.
/// </summary>
public sealed class SpaServingApiFactory : NonDevelopmentApiFactory
{
    // The marker proves the response body is this fixture's index.html, not another
    // HTML document from somewhere else in the pipeline.
    public const string IndexMarker = "crudworks-spa-fixture";

    public static readonly string IndexHtml =
        $"""
        <!doctype html>
        <html lang="en">
          <head>
            <meta charset="utf-8" />
            <title>CrudWorks SPA fixture</title>
          </head>
          <body>
            <div id="root">{IndexMarker}</div>
          </body>
        </html>
        """;

    private readonly string _webRootPath = Path.Combine(
        Path.GetTempPath(), $"crudworks-api-tests-wwwroot-{Guid.NewGuid():N}");

    public SpaServingApiFactory() : this(serveIndexHtml: true)
    {
    }

    // Internal because xunit class fixtures may expose exactly one public constructor;
    // the fail-fast test uses this overload from the same assembly.
    internal SpaServingApiFactory(bool serveIndexHtml)
    {
        Directory.CreateDirectory(_webRootPath);
        if (serveIndexHtml)
        {
            File.WriteAllText(Path.Combine(_webRootPath, "index.html"), IndexHtml);
        }
    }

    protected override IReadOnlyDictionary<string, string?> DeploymentConfiguration { get; } =
        new Dictionary<string, string?> { ["Spa:Serving"] = "true" };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // The webroot hosting setting is the only mechanism that reliably reaches the
        // minimal-hosting WebApplicationBuilder (the in-memory "webroot" configuration
        // key does not), verified experimentally on .NET 10.
        builder.UseSetting(WebHostDefaults.WebRootKey, _webRootPath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_webRootPath))
        {
            Directory.Delete(_webRootPath, recursive: true);
        }
    }
}

/// <summary>
/// Hosts the API in Production with Database:RunMigrationsAndSeed=true, the way the
/// demo deployment prepares its SQLite database at startup.
/// </summary>
public sealed class MigrationsAndSeedApiFactory : NonDevelopmentApiFactory
{
    protected override IReadOnlyDictionary<string, string?> DeploymentConfiguration { get; } =
        new Dictionary<string, string?> { ["Database:RunMigrationsAndSeed"] = "true" };
}
