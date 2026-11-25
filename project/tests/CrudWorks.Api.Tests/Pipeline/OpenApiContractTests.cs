using System.Net;
using System.Text.Json;

namespace CrudWorks.Api.Tests.Pipeline;

/// <summary>
/// Semantic assertions over the Development OpenAPI document (SPEC-005).
/// These tests describe the contract ADR-0004 already owns; they must not change
/// runtime behavior, only require the generated document to describe it accurately.
/// </summary>
public sealed class OpenApiContractTests : IClassFixture<CrudWorksApiFactory>, IClassFixture<NonDevelopmentApiFactory>
{
    private static readonly string[] TaskStatusValues = ["Todo", "InProgress", "Done"];

    private readonly CrudWorksApiFactory _factory;
    private readonly NonDevelopmentApiFactory _productionFactory;

    public OpenApiContractTests(CrudWorksApiFactory factory, NonDevelopmentApiFactory productionFactory)
    {
        _factory = factory;
        _productionFactory = productionFactory;
    }

    [Fact]
    public async Task Document_ContainsAllNineOperationsIncludingHealth()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var paths = document.RootElement.GetProperty("paths");
        var operations = paths.EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject()
                .Where(method => method.Name is "get" or "post" or "put" or "delete" or "patch")
                .Select(method => $"{method.Name.ToUpperInvariant()} {path.Name}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            "DELETE /api/tasks/{id}",
            "GET /api/auth/me",
            "GET /api/health",
            "GET /api/tasks",
            "GET /api/tasks/{id}",
            "POST /api/auth/login",
            "POST /api/auth/register",
            "POST /api/tasks",
            "PUT /api/tasks/{id}",
        };

        Assert.Equal(expected, operations);
    }

    [Fact]
    public async Task HealthOperation_DocumentsTheExisting200Body()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var health = document.RootElement.GetProperty("paths").GetProperty("/api/health").GetProperty("get");
        var schema = health.GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");

        Assert.Equal("object", schema.GetProperty("type").GetString());
        var properties = schema.GetProperty("properties");
        Assert.Equal("string", SingleType(properties.GetProperty("status")));
        Assert.Equal("number", SingleType(properties.GetProperty("totalDurationMs")));
    }

    [Fact]
    public async Task BearerScheme_IsDeclaredAndAppliedToProtectedOperations()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var scheme = document.RootElement.GetProperty("components").GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());

        foreach (var (path, method) in new[]
        {
            ("/api/tasks", "get"),
            ("/api/tasks", "post"),
            ("/api/tasks/{id}", "get"),
            ("/api/tasks/{id}", "put"),
            ("/api/tasks/{id}", "delete"),
            ("/api/auth/me", "get"),
        })
        {
            var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.True(HasBearerSecurity(operation), $"{method.ToUpperInvariant()} {path} must declare the Bearer scheme.");
        }

        foreach (var (path, method) in new[]
        {
            ("/api/auth/register", "post"),
            ("/api/auth/login", "post"),
            ("/api/health", "get"),
        })
        {
            var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.False(HasBearerSecurity(operation), $"{method.ToUpperInvariant()} {path} must stay anonymous.");
        }
    }

    [Fact]
    public async Task ProblemDetails_DocumentsTraceIdAndValidationErrors()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var schema = Schema(document, "ProblemDetails");
        var properties = schema.GetProperty("properties");

        Assert.True(properties.TryGetProperty("traceId", out var traceId));
        Assert.Contains("string", Types(traceId));

        Assert.True(properties.TryGetProperty("status", out var status));
        Assert.Contains("integer", Types(status));
        Assert.DoesNotContain("string", Types(status));

        Assert.True(properties.TryGetProperty("errors", out var errors));
        Assert.Contains("object", Types(errors));
        Assert.Equal("array", SingleType(errors.GetProperty("additionalProperties")));
        Assert.Equal("string", SingleType(errors.GetProperty("additionalProperties").GetProperty("items")));
    }

    [Fact]
    public async Task TaskStatus_IsTheStringEnumUsedOnTheWire()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var schema = Schema(document, "TaskItemStatus");
        Assert.Equal("string", SingleType(schema));
        Assert.Equal(
            TaskStatusValues,
            schema.GetProperty("enum").EnumerateArray().Select(value => value.GetString()).ToArray());
    }

    [Fact]
    public async Task TaskList_DocumentsPaginationParametersAndEnvelope()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var operation = document.RootElement.GetProperty("paths").GetProperty("/api/tasks").GetProperty("get");
        var parameters = operation.GetProperty("parameters").EnumerateArray()
            .ToDictionary(
                parameter => parameter.GetProperty("name").GetString()!,
                StringComparer.Ordinal);

        Assert.Equal("query", parameters["cursor"].GetProperty("in").GetString());
        Assert.Equal("string", SingleType(parameters["cursor"].GetProperty("schema")));

        var take = parameters["take"];
        Assert.Equal("query", take.GetProperty("in").GetString());
        Assert.Equal("integer", SingleType(take.GetProperty("schema")));
        Assert.Equal(1, take.GetProperty("schema").GetProperty("minimum").GetInt32());
        Assert.Equal(100, take.GetProperty("schema").GetProperty("maximum").GetInt32());
        Assert.Equal(10, take.GetProperty("schema").GetProperty("default").GetInt32());

        var schema = Schema(document, "TaskPageResponse");
        Assert.Equal(["items", "nextCursor"], Required(schema));

        var items = schema.GetProperty("properties").GetProperty("items");
        Assert.Equal("array", SingleType(items));
        Assert.Equal(
            "TaskResponse",
            items.GetProperty("items").GetProperty("$ref").GetString()!.Split('/')[^1]);

        var nextCursor = schema.GetProperty("properties").GetProperty("nextCursor");
        Assert.Contains("null", Types(nextCursor));
        Assert.Contains("string", Types(nextCursor));
    }

    [Fact]
    public async Task RequestSchemas_ReflectContractRequirednessAndNullability()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var create = Schema(document, "CreateTaskRequest");
        Assert.Equal(["title"], Required(create));
        Assert.Equal("string", SingleType(create.GetProperty("properties").GetProperty("title")));
        Assert.Contains("null", Types(create.GetProperty("properties").GetProperty("description")));
        Assert.Contains("null", Types(create.GetProperty("properties").GetProperty("status")));
        Assert.Equal(
            TaskStatusValues,
            create.GetProperty("properties").GetProperty("status").GetProperty("enum")
                .EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.Contains("null", Types(create.GetProperty("properties").GetProperty("dueDate")));

        var update = Schema(document, "UpdateTaskRequest");
        Assert.Equal(["status", "title"], Required(update));
        Assert.Equal("string", SingleType(update.GetProperty("properties").GetProperty("title")));
        Assert.Equal("string", SingleType(update.GetProperty("properties").GetProperty("status")));
        Assert.Equal(
            TaskStatusValues,
            update.GetProperty("properties").GetProperty("status").GetProperty("enum")
                .EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.Contains("null", Types(update.GetProperty("properties").GetProperty("description")));
        Assert.Contains("null", Types(update.GetProperty("properties").GetProperty("dueDate")));

        var register = Schema(document, "RegisterRequest");
        Assert.Equal(["displayName", "email", "password"], Required(register));
        foreach (var property in register.GetProperty("properties").EnumerateObject())
        {
            Assert.Equal("string", SingleType(property.Value));
        }

        var login = Schema(document, "LoginRequest");
        Assert.Equal(["email", "password"], Required(login));
        foreach (var property in login.GetProperty("properties").EnumerateObject())
        {
            Assert.Equal("string", SingleType(property.Value));
        }
    }

    [Fact]
    public async Task CreatedResponses_DocumentTheLocationHeader()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var paths = document.RootElement.GetProperty("paths");
        foreach (var (path, method) in new[] { ("/api/auth/register", "post"), ("/api/tasks", "post") })
        {
            var response = paths.GetProperty(path).GetProperty(method).GetProperty("responses").GetProperty("201");
            Assert.True(response.GetProperty("headers").TryGetProperty("Location", out var location));
            Assert.Equal("string", SingleType(location.GetProperty("schema")));
        }
    }

    [Fact]
    public async Task ApiOperations_DocumentContractStatusSetsAnd204NoBody()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var expected = new Dictionary<(string Path, string Method), string[]>
        {
            [("/api/health", "get")] = ["200"],
            [("/api/auth/register", "post")] = ["201", "400", "409", "500"],
            [("/api/auth/login", "post")] = ["200", "400", "401", "500"],
            [("/api/auth/me", "get")] = ["200", "401", "500"],
            [("/api/tasks", "get")] = ["200", "400", "401", "500"],
            [("/api/tasks", "post")] = ["201", "400", "401", "500"],
            [("/api/tasks/{id}", "get")] = ["200", "401", "404", "500"],
            [("/api/tasks/{id}", "put")] = ["200", "400", "401", "404", "500"],
            [("/api/tasks/{id}", "delete")] = ["204", "401", "404", "500"],
        };

        var paths = document.RootElement.GetProperty("paths");
        foreach (var ((path, method), statuses) in expected)
        {
            var responses = paths.GetProperty(path).GetProperty(method).GetProperty("responses");
            var actual = responses.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(statuses.Order(StringComparer.Ordinal).ToArray(), actual);
        }

        var deleteResponse = paths.GetProperty("/api/tasks/{id}").GetProperty("delete")
            .GetProperty("responses").GetProperty("204");
        Assert.False(deleteResponse.TryGetProperty("content", out _));
    }

    [Fact]
    public async Task ApiErrorResponses_UseProblemJsonContent()
    {
        using var document = await GetDevelopmentDocumentAsync();

        var paths = document.RootElement.GetProperty("paths");
        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject().Where(m => m.Name is "get" or "post" or "put" or "delete"))
            {
                var responses = method.Value.GetProperty("responses");
                foreach (var response in responses.EnumerateObject())
                {
                    if (response.Name is not ("400" or "401" or "404" or "409" or "500"))
                    {
                        continue;
                    }

                    Assert.Equal(
                        "application/problem+json",
                        response.Value.GetProperty("content").EnumerateObject().Single().Name);
                }
            }
        }
    }

    [Fact]
    public async Task Production_DoesNotServeTheOpenApiDocument()
    {
        using var client = _productionFactory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<JsonDocument> GetDevelopmentDocumentAsync()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static JsonElement Schema(JsonDocument document, string name) =>
        document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(name);

    private static string[] Required(JsonElement schema) =>
        schema.GetProperty("required").EnumerateArray()
            .Select(value => value.GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string[] Types(JsonElement schema)
    {
        var type = schema.GetProperty("type");
        return type.ValueKind == JsonValueKind.Array
            ? type.EnumerateArray().Select(value => value.GetString()!).ToArray()
            : [type.GetString()!];
    }

    private static string SingleType(JsonElement schema)
    {
        var types = Types(schema);
        Assert.Single(types);
        return types[0];
    }

    private static bool HasBearerSecurity(JsonElement operation) =>
        operation.TryGetProperty("security", out var security)
        && security.ValueKind == JsonValueKind.Array
        && security.EnumerateArray().Any(requirement => requirement.TryGetProperty("Bearer", out _));
}
