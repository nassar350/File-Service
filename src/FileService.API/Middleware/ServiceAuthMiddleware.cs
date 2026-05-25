using Microsoft.Extensions.Primitives;

namespace FileService.API.Middleware;

public sealed class ServiceAuthMiddleware
{
    private const string ServiceTokenHeader = "x-service-token";

    private readonly RequestDelegate _next;

    public ServiceAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var configuredToken = configuration["ServiceAuth:Token"];
        if (string.IsNullOrWhiteSpace(configuredToken))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { error = "Service authentication is not configured" });
            return;
        }

        if (!context.Request.Headers.TryGetValue(ServiceTokenHeader, out StringValues headerValue)
            || StringValues.IsNullOrEmpty(headerValue)
            || string.IsNullOrWhiteSpace(headerValue.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing service authentication token" });
            return;
        }

        if (!string.Equals(headerValue.ToString().Trim(), configuredToken.Trim(), StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid service authentication token" });
            return;
        }

        await _next(context);
    }
}