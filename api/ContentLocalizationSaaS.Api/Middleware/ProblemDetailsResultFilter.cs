using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ContentLocalizationSaaS.Api.Middleware;

public sealed class ProblemDetailsResultFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.StatusCode is >= 400 and <= 599)
        {
            if (objectResult.Value is not ProblemDetails and not ValidationProblemDetails)
            {
                var status = objectResult.StatusCode ?? StatusCodes.Status500InternalServerError;
                var title = status switch
                {
                    400 => "Bad request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    404 => "Not found",
                    409 => "Conflict",
                    _ when status >= 500 => "Unexpected server error",
                    _ => "Request failed"
                };

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Type = $"https://httpstatuses.com/{status}",
                    Detail = TryExtractDetail(objectResult.Value)
                };

                TryCopyExtensions(problem, objectResult.Value);
                objectResult.Value = problem;
                objectResult.DeclaredType = typeof(ProblemDetails);
                objectResult.ContentTypes.Clear();
                objectResult.ContentTypes.Add("application/problem+json");
            }
        }

        return next();
    }

    private static string? TryExtractDetail(object? value)
    {
        if (value is null) return null;

        var errorProp = value.GetType().GetProperty("error");
        var guidanceProp = value.GetType().GetProperty("guidance");
        var messageProp = value.GetType().GetProperty("message");

        var error = errorProp?.GetValue(value)?.ToString();
        var guidance = guidanceProp?.GetValue(value)?.ToString();
        var message = messageProp?.GetValue(value)?.ToString();

        return guidance ?? message ?? error;
    }

    private static void TryCopyExtensions(ProblemDetails problem, object? value)
    {
        if (value is null) return;

        var props = value.GetType().GetProperties();
        foreach (var prop in props)
        {
            var name = prop.Name;
            if (name is "Status" or "Title" or "Type" or "Detail" or "Instance") continue;

            var propValue = prop.GetValue(value);
            if (propValue is not null)
            {
                problem.Extensions[name] = propValue;
            }
        }
    }
}
