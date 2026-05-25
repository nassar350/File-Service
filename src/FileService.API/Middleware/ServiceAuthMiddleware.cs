using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace FileService.API.Middleware;

public sealed class ServiceAuthMiddleware
{
    private const string ServiceTokenHeader = "x-service-token";
    private static readonly Regex CommitUploadPath = new("^/v1/uploads/sessions/[^/]+/commit$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex DeleteFilePath = new("^/v1/files/[^/]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly RequestDelegate _next;

    public ServiceAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (!RequiresServiceToken(context.Request.Method, context.Request.Path.Value))
        {
            await _next(context);
            return;
        }

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

    private static bool RequiresServiceToken(string? method, string? path)
    {
        if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) && CommitUploadPath.IsMatch(path)
            || string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase) && DeleteFilePath.IsMatch(path);
    }
}