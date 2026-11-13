using CrudWorks.Api.Authentication;
using CrudWorks.Api.ErrorHandling;
using CrudWorks.Api.Security;
using CrudWorks.Application.Auth;
using CrudWorks.Application.Common;
using CrudWorks.Application.Tasks;
using CrudWorks.Application.Users;
using CrudWorks.Infrastructure.Persistence;
using CrudWorks.Infrastructure.Persistence.Seed;
using CrudWorks.Infrastructure.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ADR-0004 §1: enums and dates travel as strings ("Todo", "InProgress", "Done").
// Default naming keeps the exact contract names; camelCase applies to properties.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<CrudWorksDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=crudworks.db";
    options.UseSqlite(connectionString);
});

// Registered through a factory so the key is resolved after configuration is final:
// a warning is logged for the Development fallback and a missing key fails startup
// outside Development (the settings are resolved eagerly below).
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

    var (key, isEphemeral) = JwtSigningKey.Resolve(jwtOptions.SigningKey, environment);
    if (isEphemeral)
    {
        serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Jwt")
            .LogWarning(
                "Jwt:SigningKey is not configured; using an ephemeral Development key. Tokens will not survive a restart.");
    }

    jwtOptions.SigningKey = key;
    return jwtOptions;
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<ILoginAudit, LoginAudit>();

builder.Services.AddJwtBearerAuthentication();

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
// ADR-0004 §6: configured origin, explicit methods and headers, no credentials.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigin)
            .WithHeaders("Content-Type", "Authorization")
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS"));
});

var app = builder.Build();

// Fail fast when the signing key is missing outside Development.
app.Services.GetRequiredService<JwtOptions>();

// Deployment flags for the Production demo host (Render), both off by default so local
// development and the API-only pipeline are unchanged:
// - Spa:Serving serves the built React app from wwwroot on this same origin.
// - Database:RunMigrationsAndSeed applies migrations and the demo seed outside Development.
var spaServing = app.Configuration.GetValue<bool>("Spa:Serving");

app.UseExceptionHandler();

if (spaServing)
{
    var spaIndexPath = ResolveSpaIndexPath(app.Environment);
    if (!File.Exists(spaIndexPath))
    {
        // A missing frontend build would otherwise surface as a blank page on every
        // route; fail at startup with the reason instead.
        throw new InvalidOperationException(
            $"Spa:Serving is enabled but the SPA entry point was not found at '{spaIndexPath}'. "
            + "Copy the frontend build output into wwwroot before enabling SPA serving.");
    }

    app.UseDefaultFiles();
    app.UseStaticFiles();
}

// Routing-level failures (unknown route, wrong method) have no body of their own.
// ADR-0004 §1 requires every error, including those, to be RFC 7807 problem+json.
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.HasStarted || response.ContentLength is not null || response.ContentType is not null)
    {
        return;
    }

    var problem = ApiProblemDetails.Create(
        context.HttpContext,
        response.StatusCode,
        StatusTitle(response.StatusCode));

    await response.WriteAsJsonAsync(problem, options: null, contentType: ApiProblemDetails.ContentType);
});
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Development always migrates and seeds locally. Any other environment opts in through
// Database:RunMigrationsAndSeed so a container host can prepare its database at startup;
// a database error then fails startup, like a missing signing key does.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:RunMigrationsAndSeed"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CrudWorksDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DatabaseSeeder.InitializeAsync(dbContext, passwordHasher);
}

app.MapControllers();
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds
        });
    }
});

if (spaServing)
{
    // Cached once: the SPA build ships inside the image and does not change at runtime.
    var spaIndexHtml = await File.ReadAllTextAsync(ResolveSpaIndexPath(app.Environment));

    // Last in the route table: everything the API routes and static files did not
    // handle. Unknown /api routes keep the RFC 7807 404 contract; any other path is a
    // React Router deep link and gets index.html.
    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            const int status = StatusCodes.Status404NotFound;
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(
                ApiProblemDetails.Create(context, status, StatusTitle(status)),
                options: null,
                contentType: ApiProblemDetails.ContentType,
                context.RequestAborted);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(spaIndexHtml, context.RequestAborted);
    });
}

app.Run();

// Titles for routing-level failures, which never reach a controller.
static string StatusTitle(int status) => status switch
{
    400 => "Bad request.",
    401 => "Authentication required.",
    403 => "Forbidden.",
    404 => "Resource not found.",
    405 => "Method not allowed.",
    _ => "The request could not be completed.",
};

// WebRootPath is null when no wwwroot directory exists (a source checkout without a
// frontend build), so fall back to the conventional location.
static string ResolveSpaIndexPath(IWebHostEnvironment environment) =>
    Path.Combine(
        environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
        "index.html");

public partial class Program { }
