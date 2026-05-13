using Hangfire;
using Microsoft.Extensions.Hosting;

namespace FileService.Infrastructure.Jobs;

public sealed class ExpiredStagingCleanupScheduler : IHostedService
{
    private readonly IRecurringJobManager _recurringJobManager;

    public ExpiredStagingCleanupScheduler(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _recurringJobManager.AddOrUpdate<ExpiredStagingCleanupJob>(
            "cleanup-expired-staged-files",
            job => job.RunAsync(JobCancellationToken.Null),
            Cron.Daily());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
