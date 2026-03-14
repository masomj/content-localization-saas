using ContentLocalizationSaaS.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var actor = context.Request.Headers["X-Actor-Email"].ToString();
        var projectId = ResolveProjectId(context);
        var endpoint = $"{context.Request.Method} {context.Request.Path}";

        logger.LogError(
            exception,
            "Unhandled exception at {Endpoint} (actor={Actor}, projectId={ProjectId}, traceId={TraceId})",
            endpoint,
            string.IsNullOrWhiteSpace(actor) ? "unknown" : actor,
            projectId ?? "unknown",
            context.TraceIdentifier);

        var (statusCode, payload) = exception switch
        {
            RequestValidationException validation => (
                StatusCodes.Status400BadRequest,
                new ValidationProblemDetails(validation.Errors.ToDictionary(x => x.Key, x => x.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed"
                }),
            ResourceNotFoundException => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found"
                }),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Unexpected server error"
                })
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(payload);
    }

    private static string? ResolveProjectId(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("projectId", out var queryProjectId))
        {
            var value = queryProjectId.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        if (context.Request.RouteValues.TryGetValue("projectId", out var routeProjectId) && routeProjectId is not null)
        {
            var value = routeProjectId.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }
}

public static class ApiExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseApiExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ApiExceptionMiddleware>();
}
