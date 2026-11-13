using System.Net;
using System.Net.Http.Json;

namespace CrudWorks.Api.Tests;

public sealed class HealthTests(CrudWorksApiFactory factory) : IClassFixture<CrudWorksApiFactory>
{
    [Fact]
    public async Task GetHealth_ReturnsOkAnonymously()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ReturnsTheContractShape()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        // ADR-0004 §2 pins the body: { "status": "Healthy", "totalDurationMs": 1.42 }.
        var body = await response.Content.ReadFromJsonAsync<HealthResource>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
        Assert.True(body.TotalDurationMs >= 0);
    }

    private sealed record HealthResource(string Status, double TotalDurationMs);
}
