using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.Storage;

public sealed class S3ConfigHealthCheck : IHealthCheck
{
    private readonly S3StorageOptions _options;

    public S3ConfigHealthCheck(IOptions<S3StorageOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Storage:S3:BucketName is required."));
        }

        if (string.IsNullOrWhiteSpace(_options.AccessKey) != string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Storage:S3:AccessKey and Storage:S3:SecretKey must be set together."));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
