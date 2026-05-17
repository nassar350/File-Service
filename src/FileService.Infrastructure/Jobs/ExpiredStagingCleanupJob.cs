using FileService.Application.Abstractions.Repositories;
using FileService.Application.Abstractions.Storage;
using FileService.Domain.Enums;
using FileService.Domain.Files;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FileService.Infrastructure.Jobs;

public sealed class ExpiredStagingCleanupJob
{
    private const int BatchSize = 100;

    private readonly IFileRepository _fileRepository;
    private readonly IFileEventRepository _fileEventRepository;
    private readonly IFileCleanupLogRepository _cleanupLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageProviderResolver _storageResolver;
    private readonly ILogger<ExpiredStagingCleanupJob> _logger;

    public ExpiredStagingCleanupJob(
        IFileRepository fileRepository,
        IFileEventRepository fileEventRepository,
        IFileCleanupLogRepository cleanupLogRepository,
        IUnitOfWork unitOfWork,
        IFileStorageProviderResolver storageResolver,
        ILogger<ExpiredStagingCleanupJob> logger)
    {
        _fileRepository = fileRepository;
        _fileEventRepository = fileEventRepository;
        _cleanupLogRepository = cleanupLogRepository;
        _unitOfWork = unitOfWork;
        _storageResolver = storageResolver;
        _logger = logger;
    }

    public async Task RunAsync(IJobCancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var files = await _fileRepository.GetExpiredStagedAsync(now, BatchSize, cancellationToken.ShutdownToken);

        foreach (var file in files)
        {
            try
            {
                var storageProvider = _storageResolver.Resolve(file.Provider);
                await storageProvider.DeleteAsync(file.ProviderKey, cancellationToken.ShutdownToken);

                file.Status = FileStatus.Deleted;
                file.DeletedAt = now;
                file.StagingExpiresAt = null;
                file.UpdatedAt = now;

                await _fileRepository.UpdateAsync(file, cancellationToken.ShutdownToken);
                await _fileEventRepository.AddAsync(new FileEvent
                {
                    Id = Guid.NewGuid(),
                    FileId = file.Id,
                    EventType = FileEventType.Deleted,
                    CreatedAt = now
                }, cancellationToken.ShutdownToken);

                await AddCleanupLogAsync(file.Id, "cleanup-expired", FileCleanupLogStatus.Succeeded, null, now, cancellationToken.ShutdownToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup staged file {FileId}", file.Id);
                await AddCleanupLogAsync(file.Id, "cleanup-expired", FileCleanupLogStatus.Failed, ex.Message, now, cancellationToken.ShutdownToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken.ShutdownToken);
    }

    private Task AddCleanupLogAsync(
        Guid fileId,
        string action,
        FileCleanupLogStatus status,
        string? message,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var log = new FileCleanupLog
        {
            Id = Guid.NewGuid(),
            FileId = fileId,
            Action = action,
            Status = status,
            Message = message,
            CreatedAt = timestamp
        };

        return _cleanupLogRepository.AddAsync(log, cancellationToken);
    }
}
