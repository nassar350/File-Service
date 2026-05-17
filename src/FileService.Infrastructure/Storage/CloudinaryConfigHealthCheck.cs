using Microsoft.Extensions.Diagnostics.HealthChecks;
using FileService.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.Storage;

public sealed class CloudinaryConfigHealthCheck : IHealthCheck
{
    private readonly CloudinaryStorageOptions _options;

    public CloudinaryConfigHealthCheck(IOptions<CloudinaryStorageOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.CloudName)
            || string.IsNullOrWhiteSpace(_options.ApiKey)
            || string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Cloudinary configuration is missing."));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
