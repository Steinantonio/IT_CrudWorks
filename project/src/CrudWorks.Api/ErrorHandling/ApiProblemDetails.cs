using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CrudWorks.Api.ErrorHandling;

internal static class ApiProblemDetails
{
    public const string ContentType = "application/problem+json";

    public static ProblemDetails Create(
        HttpContext httpContext,
        int status,
        string title,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = TypeFor(status),
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        return problem;
    }

    private static string TypeFor(int status) => status switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
    };
}
