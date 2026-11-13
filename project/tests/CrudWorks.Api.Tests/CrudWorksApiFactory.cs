using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CrudWorks.Api.Tests;

/// <summary>
/// Hosts the real API pipeline against a database that belongs to this factory only:
/// migrations and the Development seed run exactly as they do at startup.
/// </summary>
public sealed class CrudWorksApiFactory : WebApplicationFactory<Program>
{
    // 64 bytes of 'k', base64 encoded: fixed, non-secret key for tests only.
    public const string TestSigningKey =
        "a2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2tra2traw==";

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"crudworks-api-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = TestSigningKey,
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
