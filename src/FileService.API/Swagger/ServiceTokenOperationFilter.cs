using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FileService.API.Swagger;

public sealed class ServiceTokenOperationFilter : IOperationFilter
{
    private const string SecuritySchemeName = "ServiceToken";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!RequiresServiceToken(context.ApiDescription.HttpMethod, context.ApiDescription.RelativePath))
        {
            return;
        }

        operation.Responses ??= [];

        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add("401", new OpenApiResponse { Description = "Missing service authentication token" });
        }

        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add("403", new OpenApiResponse { Description = "Invalid service authentication token" });
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SecuritySchemeName, context.Document)] = []
        });
    }

    private static bool RequiresServiceToken(string? method, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        return string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
            && string.Equals(relativePath, "v1/uploads/sessions/{fileId}/commit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase)
            && string.Equals(relativePath, "v1/files/{fileId}", StringComparison.OrdinalIgnoreCase);
    }
}