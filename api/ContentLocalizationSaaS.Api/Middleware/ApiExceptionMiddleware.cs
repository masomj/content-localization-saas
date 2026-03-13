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
        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);

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
}

public static class ApiExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseApiExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ApiExceptionMiddleware>();
}
