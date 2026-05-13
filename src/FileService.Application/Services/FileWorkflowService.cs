using FileService.Application.Abstractions.Repositories;
using FileService.Application.Abstractions.Services;
using FileService.Application.Abstractions.Storage;
using FileService.Application.Models;
using FileService.Domain.Enums;
using FileService.Domain.Files;
using Microsoft.Extensions.Options;

namespace FileService.Application.Services;

public sealed class FileWorkflowService : IFileWorkflowService
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileEventRepository _fileEventRepository;
    private readonly IFileCleanupLogRepository _fileCleanupLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageProviderResolver _storageResolver;
    private readonly StagingOptions _stagingOptions;

    public FileWorkflowService(
        IFileRepository fileRepository,
        IFileEventRepository fileEventRepository,
        IFileCleanupLogRepository fileCleanupLogRepository,
        IUnitOfWork unitOfWork,
        IFileStorageProviderResolver storageResolver,
        IOptions<StagingOptions> stagingOptions)
    {
        _fileRepository = fileRepository;
        _fileEventRepository = fileEventRepository;
        _fileCleanupLogRepository = fileCleanupLogRepository;
        _unitOfWork = unitOfWork;
        _storageResolver = storageResolver;
        _stagingOptions = stagingOptions.Value;
    }

    public async Task<FileRecord> StageUploadAsync(
        StageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var storageProvider = request.Provider.HasValue
            ? _storageResolver.Resolve(request.Provider.Value)
            : _storageResolver.ResolveDefault();

        var uploadResult = await storageProvider.UploadAsync(
            request.Content,
            request.FileName,
            request.MimeType,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var ttlMinutes = request.StagingTtlMinutes ?? _stagingOptions.TtlMinutes;
        var expiresAt = ttlMinutes > 0 ? now.AddMinutes(ttlMinutes) : (DateTimeOffset?)null;

        var file = new FileRecord
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            OwnerId = request.OwnerId,
            Purpose = request.Purpose,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Provider = storageProvider.Provider,
            ProviderKey = uploadResult.ProviderKey,
            StorageRegion = uploadResult.StorageRegion ?? string.Empty,
            OriginalFilename = request.FileName,
            MimeType = uploadResult.MimeType ?? request.MimeType,
            Size = uploadResult.Size,
            Checksum = uploadResult.Checksum ?? string.Empty,
            Metadata = request.Metadata,
            Status = FileStatus.Staged,
            StagingExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        var fileEvent = new FileEvent
        {
            Id = Guid.NewGuid(),
            FileId = file.Id,
            EventType = FileEventType.Staged,
            Metadata = request.Metadata,
            CreatedAt = now
        };

        await _fileRepository.AddAsync(file, cancellationToken);
        await _fileEventRepository.AddAsync(fileEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return file;
    }

    public async Task<FileRecord> CommitUploadAsync(
        CommitUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken)
            ?? throw new InvalidOperationException("File not found.");

        if (file.Status != FileStatus.Staged)
        {
            throw new InvalidOperationException("Only staged files can be committed.");
        }

        var storageProvider = _storageResolver.Resolve(file.Provider);
        var promotion = await storageProvider.PromoteAsync(
            file.ProviderKey,
            request.DestinationFolder,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;

        file.ProviderKey = promotion.ProviderKey;
        if (!string.IsNullOrWhiteSpace(promotion.StorageRegion))
        {
            file.StorageRegion = promotion.StorageRegion;
        }

        file.Status = FileStatus.Committed;
        file.CommittedAt = now;
        file.StagingExpiresAt = null;
        file.UpdatedAt = now;

        await _fileRepository.UpdateAsync(file, cancellationToken);

        await _fileEventRepository.AddAsync(new FileEvent
        {
            Id = Guid.NewGuid(),
            FileId = file.Id,
            EventType = FileEventType.Committed,
            CreatedAt = now
        }, cancellationToken);

        if (request.ReplacesFileId.HasValue)
        {
            await ReplaceCommittedFileAsync(request.ReplacesFileId.Value, now, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return file;
    }

    public async Task<FileRecord> ReplaceUploadAsync(
        ReplaceUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken)
            ?? throw new InvalidOperationException("File not found.");

        if (existing.Status != FileStatus.Staged)
        {
            throw new InvalidOperationException("Only staged files can be replaced.");
        }

        var now = DateTimeOffset.UtcNow;

        var storageProvider = _storageResolver.Resolve(existing.Provider);
        await storageProvider.DeleteAsync(existing.ProviderKey, cancellationToken);
        await AddCleanupLogAsync(existing.Id, "replace-staged", FileCleanupLogStatus.Succeeded, null, now, cancellationToken);
        existing.Status = FileStatus.Deleted;
        existing.DeletedAt = now;
        existing.UpdatedAt = now;

        await _fileRepository.UpdateAsync(existing, cancellationToken);
        await _fileEventRepository.AddAsync(new FileEvent
        {
            Id = Guid.NewGuid(),
            FileId = existing.Id,
            EventType = FileEventType.Aborted,
            CreatedAt = now
        }, cancellationToken);

        var uploadResult = await storageProvider.UploadAsync(
            request.Content,
            request.FileName,
            request.MimeType,
            cancellationToken);

        var ttlMinutes = _stagingOptions.TtlMinutes;
        var expiresAt = ttlMinutes > 0 ? now.AddMinutes(ttlMinutes) : (DateTimeOffset?)null;

        var newFile = new FileRecord
        {
            Id = Guid.NewGuid(),
            TenantId = existing.TenantId,
            OwnerId = existing.OwnerId,
            Purpose = existing.Purpose,
            EntityType = existing.EntityType,
            EntityId = existing.EntityId,
            Provider = existing.Provider,
            ProviderKey = uploadResult.ProviderKey,
            StorageRegion = uploadResult.StorageRegion ?? string.Empty,
            OriginalFilename = request.FileName,
            MimeType = uploadResult.MimeType ?? request.MimeType,
            Size = uploadResult.Size,
            Checksum = uploadResult.Checksum ?? string.Empty,
            Metadata = request.Metadata,
            Status = FileStatus.Staged,
            StagingExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _fileRepository.AddAsync(newFile, cancellationToken);
        await _fileEventRepository.AddAsync(new FileEvent
        {
            Id = Guid.NewGuid(),
            FileId = newFile.Id,
            EventType = FileEventType.Staged,
            Metadata = request.Metadata,
            CreatedAt = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return newFile;
    }

    public async Task AbortUploadAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken)
            ?? throw new InvalidOperationException("File not found.");

        if (file.Status != FileStatus.Staged)
        {
            throw new InvalidOperationException("Only staged files can be aborted.");
        }

        var now = DateTimeOffset.UtcNow;

        var storageProvider = _storageResolver.Resolve(file.Provider);
        await storageProvider.DeleteAsync(file.ProviderKey, cancellationToken);
        await AddCleanupLogAsync(file.Id, "abort", FileCleanupLogStatus.Succeeded, null, now, cancellationToken);
        file.Status = FileStatus.Deleted;
        file.DeletedAt = now;
        file.UpdatedAt = now;

        await _fileRepository.UpdateAsync(file, cancellationToken);
        await _fileEventRepository.AddAsync(new FileEvent
        {
            Id = Guid.NewGuid(),
            FileId = file.Id,
            EventType = FileEventType.Aborted,
            CreatedAt = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFileAsync(DeleteFileRequest request, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken)
            ?? throw new InvalidOperationException("File not found.");

        var now = DateTimeOffset.UtcNow;

        var storageProvider = _storageResolver.Resolve(file.Provider);
        await storageProvider.DeleteAsync(file.ProviderKey, cancellationToken);
        await AddCleanupLogAsync(file.Id, "delete", FileCleanupLogStatus.Succeeded, null, now, cancellationToken);
        file.Status = FileStatus.Deleted;
        file.DeletedAt = now;
        file.UpdatedAt = now;

        await _fileRepository.UpdateAsync(file, cancellationToken);
        await _fileEventRepository.AddAsync(new FileEvent
        {
            Id = Guid.NewGuid(),
            FileId = file.Id,
            EventType = FileEventType.Deleted,
            CreatedAt = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<FileRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _fileRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<FileRecord>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return _fileRepository.GetByIdsAsync(ids, cancellationToken);
    }

    private async Task ReplaceCommittedFileAsync(
        Guid fileId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        var storageProvider = _storageResolver.Resolve(existing.Provider);
        await storageProvider.DeleteAsync(existing.ProviderKey, cancellationToken);
        await AddCleanupLogAsync(existing.Id, "replace-committed", FileCleanupLogStatus.Succeeded, null, now, cancellationToken);

        existing.Status = FileStatus.Deleted;
        existing.DeletedAt = now;
        existing.UpdatedAt = now;

        await _fileRepository.UpdateAsync(existing, cancellationToken);
        await _fileEventRepository.AddAsync(new FileEvent
        {
            Id = Guid.NewGuid(),
            FileId = existing.Id,
            EventType = FileEventType.Deleted,
            CreatedAt = now
        }, cancellationToken);
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

        return _fileCleanupLogRepository.AddAsync(log, cancellationToken);
    }
}
