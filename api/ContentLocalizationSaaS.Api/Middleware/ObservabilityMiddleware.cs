namespace ContentLocalizationSaaS.Api.Middleware;

public sealed class ObservabilityMiddleware(RequestDelegate next, ILogger<ObservabilityMiddleware> logger)
{
    public const string CorrelationHeader = "X-Correlation-Id";

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Response.Headers[CorrelationHeader] = correlationId;
        context.Items[CorrelationHeader] = correlationId;

        var scope = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.ToString(),
            ["RequestMethod"] = context.Request.Method
        };
        using (logger.BeginScope(scope))
        {
            var started = DateTime.UtcNow;
            await next(context);
            var elapsedMs = (DateTime.UtcNow - started).TotalMilliseconds;

            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms (CorrelationId={CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                Math.Round(elapsedMs, 2),
                correlationId);
        }
    }
}

public static class ObservabilityMiddlewareExtensions
{
    public static IApplicationBuilder UseObservabilityMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ObservabilityMiddleware>();
}
