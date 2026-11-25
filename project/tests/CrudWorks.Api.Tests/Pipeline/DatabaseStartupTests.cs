using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CrudWorks.Application.Auth;
using CrudWorks.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Mvc;

namespace CrudWorks.Api.Tests.Pipeline;

/// <summary>
/// Covers Database:RunMigrationsAndSeed — the opt-in that lets a non-Development host
/// (the demo deployment) apply migrations and the demo seed before serving. Development
/// keeps its existing behavior of always migrating and seeding at startup.
/// </summary>
public sealed class DatabaseStartupTests
{
    [Fact]
    public async Task RunMigrationsAndSeed_EnabledOutsideDevelopment_MigratesAndSeedsTheDemoAccount()
    {
        using var factory = new MigrationsAndSeedApiFactory();
        using var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(DatabaseSeeder.DemoEmail, DatabaseSeeder.DemoPassword));

        // Outside Development nothing migrates unless the flag is set — there is not
        // even a users table. A 200 login proves migrations and the seed both ran
        // before the host started serving.
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginBody);

        // The demo seed also owns the five demo tasks for the demo user.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.AccessToken);
        var tasks = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, tasks.StatusCode);
        var page = await tasks.Content.ReadFromJsonAsync<TaskPageResource>();
        Assert.NotNull(page);
        Assert.True(page.Items.Count >= 5, $"Expected at least five seeded tasks, got {page.Items.Count}.");

        // The flag created the database file itself.
        Assert.True(File.Exists(factory.DatabasePath));
    }

    [Fact]
    public async Task RunMigrationsAndSeed_DisabledOutsideDevelopment_StartsWithoutTouchingTheDatabase()
    {
        using var factory = new NonDevelopmentApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        // Opt-in, not default: the host serves normally and never connects, so no
        // SQLite file is ever created.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(File.Exists(factory.DatabasePath));
    }

    private sealed record TaskResource(
        Guid Id,
        string Title,
        string? Description,
        string Status,
        DateOnly? DueDate,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);

    private sealed record TaskPageResource(
        IReadOnlyList<TaskResource> Items,
        string? NextCursor);
}
