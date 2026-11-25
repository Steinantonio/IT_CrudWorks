using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using CrudWorks.Application.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CrudWorks.Api.OpenApi;

/// <summary>
/// Adds the contract metadata that the generated OpenAPI document needs to match
/// ADR-0004. This is documentation-only: it does not change routes, validation,
/// authentication, status codes, or any other runtime behavior.
/// </summary>
internal sealed class ContractOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    private const string BearerSchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT access token returned by POST /api/auth/login.",
        };

        AddHealthOperation(document);
        AdjustSchemas(document);

        return Task.CompletedTask;
    }

    private static void AddHealthOperation(OpenApiDocument document)
    {
        document.Paths ??= new OpenApiPaths();
        document.Paths["/api/health"] = new OpenApiPathItem
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Get] = new OpenApiOperation
                {
                    Summary = "Health check",
                    Description = "Reports process health. It does not probe the database.",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Healthy",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema>
                                        {
                                            ["status"] = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.String,
                                                Description = "Health status reported by the API.",
                                            },
                                            ["totalDurationMs"] = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Number,
                                                Description = "Time spent evaluating the health report.",
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            },
        };
    }

    private static void AdjustSchemas(OpenApiDocument document)
    {
        if (document.Components?.Schemas is not { } schemas)
        {
            return;
        }

        if (schemas.TryGetValue("ProblemDetails", out var problemDetails)
            && problemDetails is OpenApiSchema problemDetailsSchema)
        {
            problemDetailsSchema.Properties ??= new Dictionary<string, IOpenApiSchema>();
            problemDetailsSchema.Properties["status"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer | JsonSchemaType.Null,
                Format = "int32",
            };
            problemDetailsSchema.Properties["traceId"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String | JsonSchemaType.Null,
                Description = "Identifier used to correlate the response with server logs.",
            };
            problemDetailsSchema.Properties["errors"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object | JsonSchemaType.Null,
                Description = "Field-level validation errors, present only for validation failures.",
                AdditionalProperties = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchema { Type = JsonSchemaType.String },
                },
            };
        }

        if (schemas.ContainsKey("TaskItemStatus"))
        {
            schemas["TaskItemStatus"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "Task status values, serialized as strings (ADR-0004).",
                Enum = new List<JsonNode>
                {
                    JsonValue.Create("Todo")!,
                    JsonValue.Create("InProgress")!,
                    JsonValue.Create("Done")!,
                },
            };
        }

        ConfigureRequest(
            schemas,
            "CreateTaskRequest",
            required: ["title"],
            nonNullable: ["title"]);

        ConfigureRequest(
            schemas,
            "UpdateTaskRequest",
            required: ["title", "status"],
            nonNullable: ["title", "status"]);

        ConfigureRequest(
            schemas,
            "RegisterRequest",
            required: ["email", "displayName", "password"],
            nonNullable: ["email", "displayName", "password"]);

        ConfigureRequest(
            schemas,
            "LoginRequest",
            required: ["email", "password"],
            nonNullable: ["email", "password"]);

        ApplyStatusEnum(schemas, "CreateTaskRequest", nullable: true);
        ApplyStatusEnum(schemas, "UpdateTaskRequest", nullable: false);
    }

    private static void ApplyStatusEnum(
        IDictionary<string, IOpenApiSchema> schemas,
        string schemaName,
        bool nullable)
    {
        if (!schemas.TryGetValue(schemaName, out var schema)
            || schema is not OpenApiSchema requestSchema
            || requestSchema.Properties is not { } properties)
        {
            return;
        }

        properties["status"] = new OpenApiSchema
        {
            Type = nullable
                ? JsonSchemaType.String | JsonSchemaType.Null
                : JsonSchemaType.String,
            Enum = new List<JsonNode>
            {
                JsonValue.Create("Todo")!,
                JsonValue.Create("InProgress")!,
                JsonValue.Create("Done")!,
            },
        };
    }

    private static void ConfigureRequest(
        IDictionary<string, IOpenApiSchema> schemas,
        string schemaName,
        string[] required,
        string[] nonNullable)
    {
        if (!schemas.TryGetValue(schemaName, out var schema) || schema is not OpenApiSchema requestSchema)
        {
            return;
        }

        requestSchema.Required = new HashSet<string>(required, StringComparer.Ordinal);

        if (requestSchema.Properties is not { } properties)
        {
            return;
        }

        foreach (var propertyName in nonNullable)
        {
            if (properties.TryGetValue(propertyName, out var property) && property is OpenApiSchema propertySchema)
            {
                propertySchema.Type = WithoutNull(propertySchema.Type);
            }
        }
    }

    private static JsonSchemaType? WithoutNull(JsonSchemaType? type) =>
        type is null ? null : type & ~JsonSchemaType.Null;
}

/// <summary>
/// Adds bearer security, Location headers, and generic 500 responses to the
/// generated operations. Documentation-only, like the document transformer.
/// </summary>
internal sealed class ContractOpenApiOperationTransformer : IOpenApiOperationTransformer
{
    private const string BearerSchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var path = "/" + (context.Description.RelativePath ?? string.Empty).TrimStart('/');

        if (IsProtected(path))
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerSchemeName, context.Document)] = [],
                },
            ];
        }

        operation.Responses ??= new OpenApiResponses();

        if (path == "/api/tasks")
        {
            NormalizeTaskPaginationParameters(operation);
        }

        foreach (var errorStatus in new[] { "400", "401", "404", "409" })
        {
            if (operation.Responses.TryGetValue(errorStatus, out var error)
                && error is OpenApiResponse errorResponse)
            {
                errorResponse.Content = ProblemJsonContent();
            }
        }

        if (operation.Responses.TryGetValue("201", out var created) && created is OpenApiResponse createdResponse)
        {
            createdResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
            createdResponse.Headers["Location"] = new OpenApiHeader
            {
                Description = "URL of the created resource.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            };
        }

        if (path != "/api/health" && !operation.Responses.ContainsKey("500"))
        {
            operation.Responses["500"] = new OpenApiResponse
            {
                Description = "Unexpected error.",
                Content = ProblemJsonContent(),
            };
        }

        return Task.CompletedTask;
    }

    private static Dictionary<string, OpenApiMediaType> ProblemJsonContent() => new()
    {
        ["application/problem+json"] = new OpenApiMediaType
        {
            Schema = CreateProblemDetailsSchema(),
        },
    };

    private static bool IsProtected(string path) =>
        path == "/api/auth/me"
        || path.StartsWith("/api/tasks", StringComparison.Ordinal);

    private static void NormalizeTaskPaginationParameters(OpenApiOperation operation)
    {
        if (operation.Parameters is null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            if (parameter is OpenApiParameter { Name: "take", In: ParameterLocation.Query } take)
            {
                // The framework describes a query int as "integer or string" with a
                // pattern; the contract is an integer with a default, so generated
                // clients get a number instead of a union.
                take.Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer,
                    Format = "int32",
                    Minimum = "1",
                    Maximum = TaskService.MaxPageSize.ToString(CultureInfo.InvariantCulture),
                    Default = JsonValue.Create(TaskService.DefaultPageSize),
                };
            }
        }
    }

    private static OpenApiSchema CreateProblemDetailsSchema() => new()
    {
        Type = JsonSchemaType.Object,
        Description = "RFC 7807 problem details returned for API errors.",
        Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["type"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["title"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["status"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer | JsonSchemaType.Null,
                Format = "int32",
            },
            ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["traceId"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["errors"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object | JsonSchemaType.Null,
                AdditionalProperties = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchema { Type = JsonSchemaType.String },
                },
            },
        },
    };
}
