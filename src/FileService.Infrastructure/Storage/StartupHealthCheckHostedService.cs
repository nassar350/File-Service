using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FileService.Infrastructure.Storage;

public sealed class StartupHealthCheckHostedService : IHostedService
{
    private readonly HealthCheckService _healthChecks;

    public StartupHealthCheckHostedService(HealthCheckService healthChecks)
    {
        _healthChecks = healthChecks;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await _healthChecks.CheckHealthAsync(
            check => check.Tags.Contains("startup"),
            cancellationToken);

        if (result.Status == HealthStatus.Healthy)
        {
            return;
        }

        var failures = result.Entries
            .Where(entry => entry.Value.Status != HealthStatus.Healthy)
            .Select(entry => $"{entry.Key}: {entry.Value.Description}");

        var message = "Startup health checks failed. " + string.Join("; ", failures);
        throw new InvalidOperationException(message);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
