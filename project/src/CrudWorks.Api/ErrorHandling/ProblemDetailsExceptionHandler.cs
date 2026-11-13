using CrudWorks.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace CrudWorks.Api.ErrorHandling;

/// <summary>
/// Translates application exceptions into RFC 7807 responses. Controllers stay free
/// of try/catch blocks and never build error payloads.
/// </summary>
public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int status;
        string title;
        IReadOnlyDictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case ValidationException validation:
                status = StatusCodes.Status400BadRequest;
                title = validation.Message;
                errors = validation.Errors;
                break;
            case ConflictException conflict:
                status = StatusCodes.Status409Conflict;
                title = conflict.Message;
                break;
            case UnauthorizedException unauthorized:
                status = StatusCodes.Status401Unauthorized;
                title = unauthorized.Message;
                break;
            case NotFoundException notFound:
                status = StatusCodes.Status404NotFound;
                title = notFound.Message;
                break;
            default:
                status = StatusCodes.Status500InternalServerError;
                title = "An unexpected error occurred.";
                logger.LogError(
                    exception,
                    "Unhandled exception for {Method} {Path}.",
                    httpContext.Request.Method,
                    httpContext.Request.Path);
                break;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            ApiProblemDetails.Create(httpContext, status, title, errors),
            options: null,
            contentType: ApiProblemDetails.ContentType,
            cancellationToken);

        return true;
    }
}
