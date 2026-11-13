using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace CrudWorks.Api.Tests.Pipeline;

/// <summary>
/// Covers the same-origin SPA mode (Spa:Serving=true) used by the Production demo
/// deployment: the React build in wwwroot is served, unknown non-API routes fall back
/// to index.html for React Router deep links, and unknown /api routes keep the RFC 7807
/// 404 contract. With the flag off (the default) the pipeline behaves exactly as before.
/// </summary>
public sealed class SpaServingTests(
    CrudWorksApiFactory apiFactory,
    SpaServingApiFactory spaFactory) : IClassFixture<CrudWorksApiFactory>, IClassFixture<SpaServingApiFactory>
{
    [Fact]
    public async Task SpaServingDisabled_UnknownNonApiRoute_Returns404AsProblemJson()
    {
        using var client = apiFactory.CreateClient();

        var response = await client.GetAsync("/definitely-not-a-page");

        // The same routing-level RFC 7807 404 the /api variant gets: with the flag off,
        // nothing about the pipeline may change for local development.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Resource not found.", problem.Title);
    }

    [Fact]
    public async Task SpaServingEnabled_Root_ReturnsTheSpaIndexHtml()
    {
        using var client = spaFactory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(SpaServingApiFactory.IndexMarker, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpaServingEnabled_DeepLinkRoute_ReturnsTheSpaIndexHtml()
    {
        using var client = spaFactory.CreateClient();

        // A React Router deep link matches no API route and no static file.
        var response = await client.GetAsync("/tasks/123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(SpaServingApiFactory.IndexMarker, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpaServingEnabled_UnknownApiRoute_Returns404AsProblemJson()
    {
        using var client = spaFactory.CreateClient();

        var response = await client.GetAsync("/api/definitely-not-a-route");

        // The RFC 7807 contract for unknown /api routes must survive the SPA fallback:
        // same status, content type, and title as the non-SPA pipeline.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Resource not found.", problem.Title);
    }

    [Fact]
    public async Task SpaServingEnabled_HealthEndpoint_ReturnsTheContractUnchanged()
    {
        using var client = spaFactory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var health = await response.Content.ReadFromJsonAsync<HealthResource>();
        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
    }

    [Fact]
    public void SpaServingEnabled_WithoutIndexHtml_FailsFastAtStartup()
    {
        using var factory = new SpaServingApiFactory(serveIndexHtml: false);

        var exception = Record.Exception(() => factory.CreateClient());

        // A missing frontend build must surface as a startup error, not as a blank page
        // on every route. The message names the flag and the missing file so the
        // misconfiguration is diagnosable from the startup log alone.
        Assert.NotNull(exception);
        Assert.Contains("Spa:Serving", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("index.html", exception.ToString(), StringComparison.Ordinal);
    }

    private sealed record HealthResource(string Status, double TotalDurationMs);
}
