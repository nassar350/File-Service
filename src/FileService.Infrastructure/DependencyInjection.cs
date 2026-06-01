using Amazon.S3;
using FileService.Application.Abstractions.Repositories;
using FileService.Application.Abstractions.Services;
using FileService.Application.Abstractions.Storage;
using FileService.Application.Services;
using FileService.Application.Options;
using FileService.Infrastructure.Jobs;
using FileService.Infrastructure.Persistence;
using FileService.Infrastructure.Storage;
using FileService.Infrastructure.Options;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConnectionStringResolver.Resolve(configuration);

        services.AddDbContext<FileServiceDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(FileServiceDbContext).Assembly.FullName)));

        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IFileEventRepository, FileEventRepository>();
        services.AddScoped<IFileCleanupLogRepository, FileCleanupLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IFileWorkflowService, FileWorkflowService>();
        services.AddScoped<ExpiredStagingCleanupJob>();
        services.AddHostedService<ExpiredStagingCleanupScheduler>();

        services.Configure<StorageOptions>(
            configuration.GetSection(StorageOptions.SectionName));
        services.Configure<StagingOptions>(
            configuration.GetSection(StagingOptions.SectionName));
        services.Configure<PrivateDownloadLinkOptions>(
            configuration.GetSection(PrivateDownloadLinkOptions.SectionName));
        services.Configure<CloudinaryStorageOptions>(
            configuration.GetSection(CloudinaryStorageOptions.SectionName));
        services.Configure<S3StorageOptions>(
            configuration.GetSection(S3StorageOptions.SectionName));

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<S3StorageOptions>>().Value;
            return S3FileStorageProvider.CreateClient(options);
        });

        services.AddSingleton<IFileStorageProvider, S3FileStorageProvider>();
        services.AddSingleton<IFileStorageProvider, CloudinaryFileStorageProvider>();
        services.AddSingleton<IFileStorageProviderResolver, DefaultFileStorageProviderResolver>();

        services.AddHealthChecks()
            .AddCheck<CloudinaryConfigHealthCheck>("cloudinary_config", tags: new[] { "startup" })
            .AddCheck<S3ConfigHealthCheck>("s3_config", tags: new[] { "startup" });
        services.AddHostedService<StartupHealthCheckHostedService>();

        return services;
    }
}