using FileService.Application.Abstractions.Repositories;
using FileService.Application.Abstractions.Storage;
using FileService.Application.Models;
using FileService.Application.Options;
using FileService.Application.Services;
using FileService.Domain.Enums;
using FileService.Domain.Files;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace FileService.UnitTests.Application.Services;

public sealed class FileWorkflowServiceTests
{
    [Fact]
    public async Task StageUploadAsync_UsesDefaultTtlAndDefaultProvider()
    {
        var storageProvider = new FakeFileStorageProvider(FileProvider.Cloudinary);
        var (service, fileRepository, eventRepository, cleanupRepository, unitOfWork, resolver) = CreateSut(
            storageProvider,
            stagingTtlMinutes: 30);

        var request = new StageUploadRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "license",
            "pharmacy",
            Guid.NewGuid(),
            "license.pdf",
            "application/pdf",
            new MemoryStream(new byte[] { 1, 2, 3 }));

        var result = await service.StageUploadAsync(request);

        Assert.Equal(FileStatus.Staged, result.Status);
        Assert.Equal(storageProvider.Provider, result.Provider);
        Assert.Equal("staging/license.pdf", result.ProviderKey);
        Assert.Equal("application/pdf", result.MimeType);
        Assert.Equal(123, result.Size);
        Assert.Equal("checksum-123", result.Checksum);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
        Assert.Equal(result.CreatedAt.AddMinutes(30), result.StagingExpiresAt);

        Assert.Equal(1, resolver.ResolveDefaultCallCount);
        Assert.Equal(0, resolver.ResolveCallCount);
        Assert.Single(fileRepository.AddedFiles);
        Assert.Same(result, fileRepository.AddedFiles[0]);
        Assert.Single(eventRepository.AddedEvents);
        Assert.Equal(FileEventType.Staged, eventRepository.AddedEvents[0].EventType);
        Assert.Empty(cleanupRepository.AddedLogs);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task CommitUploadAsync_MarksFileCommittedAndUsesStagedProviderKey()
    {
        var storageProvider = new FakeFileStorageProvider(FileProvider.AmazonS3);
        var (service, fileRepository, eventRepository, cleanupRepository, unitOfWork, resolver) = CreateSut(storageProvider);
        var file = CreateStagedFile(storageProvider.Provider);
        var stagedProviderKey = file.ProviderKey;
        await fileRepository.AddAsync(file);

        var result = await service.CommitUploadAsync(new CommitUploadRequest(file.Id));

        Assert.Equal(file.Id, result.Id);
        Assert.Equal(FileStatus.Committed, result.Status);
        Assert.Equal(stagedProviderKey, storageProvider.LastPromoteStagedProviderKey);
        Assert.Equal($"Public/{file.EntityType}/{file.Purpose}/{file.EntityId}", storageProvider.LastPromoteDestinationFolder);
        Assert.False(result.IsPrivate);
        Assert.Null(result.StagingExpiresAt);
        Assert.Equal(result.CommittedAt, result.UpdatedAt);
        Assert.Equal("region-2", result.StorageRegion);

        Assert.Equal(1, resolver.ResolveCallCount);
        Assert.Equal(0, resolver.ResolveDefaultCallCount);
        Assert.Equal(1, fileRepository.UpdateCallCount);
        Assert.Single(eventRepository.AddedEvents);
        Assert.Equal(FileEventType.Committed, eventRepository.AddedEvents[0].EventType);
        Assert.Empty(cleanupRepository.AddedLogs);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void BuildCommitDestinationFolder_UsesEntityContextFolderWhenDestinationFolderIsMissing()
    {
        var file = CreateStagedFile(FileProvider.AmazonS3);

        var destinationFolder = InvokeBuildCommitDestinationFolder(file, null, false);

        Assert.Equal($"Public/{file.EntityType}/{file.Purpose}/{file.EntityId}", destinationFolder);
    }

    [Fact]
    public void BuildCommitDestinationFolder_PrefixesProvidedDestinationFolderBeforeEntityContext()
    {
        var file = CreateStagedFile(FileProvider.AmazonS3);

        var destinationFolder = InvokeBuildCommitDestinationFolder(file, "files", false);

        Assert.Equal($"Public/files/{file.EntityType}/{file.Purpose}/{file.EntityId}", destinationFolder);
    }

    [Fact]
    public async Task CommitUploadAsync_MarksFileCommittedWhenDestinationFolderIsProvided()
    {
        var storageProvider = new FakeFileStorageProvider(FileProvider.AmazonS3);
        var (service, fileRepository, eventRepository, cleanupRepository, unitOfWork, resolver) = CreateSut(storageProvider);
        var file = CreateStagedFile(storageProvider.Provider);
        var stagedProviderKey = file.ProviderKey;
        await fileRepository.AddAsync(file);

        var result = await service.CommitUploadAsync(new CommitUploadRequest(file.Id, "files"));

        Assert.Equal(FileStatus.Committed, result.Status);
        Assert.Equal(stagedProviderKey, storageProvider.LastPromoteStagedProviderKey);
        Assert.Equal($"Public/files/{file.EntityType}/{file.Purpose}/{file.EntityId}", storageProvider.LastPromoteDestinationFolder);
        Assert.Null(result.StagingExpiresAt);
        Assert.Equal(result.CommittedAt, result.UpdatedAt);
        Assert.Equal("region-2", result.StorageRegion);

        Assert.Equal(1, resolver.ResolveCallCount);
        Assert.Equal(1, fileRepository.UpdateCallCount);
        Assert.Single(eventRepository.AddedEvents);
        Assert.Empty(cleanupRepository.AddedLogs);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task CommitUploadAsync_UsesPrivatePrefixWhenRequested()
    {
        var storageProvider = new FakeFileStorageProvider(FileProvider.AmazonS3);
        var (service, fileRepository, eventRepository, cleanupRepository, unitOfWork, resolver) = CreateSut(storageProvider);
        var file = CreateStagedFile(storageProvider.Provider);
        var stagedProviderKey = file.ProviderKey;
        await fileRepository.AddAsync(file);

        var result = await service.CommitUploadAsync(new CommitUploadRequest(file.Id, null, null, true));

        Assert.True(result.IsPrivate);
        Assert.Equal(stagedProviderKey, storageProvider.LastPromoteStagedProviderKey);
        Assert.Equal($"Private/{file.EntityType}/{file.Purpose}/{file.EntityId}", storageProvider.LastPromoteDestinationFolder);
        Assert.Equal(1, resolver.ResolveCallCount);
        Assert.Equal(1, fileRepository.UpdateCallCount);
        Assert.Single(eventRepository.AddedEvents);
        Assert.Empty(cleanupRepository.AddedLogs);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetDownloadUrlAsync_UsesPublicUrlForPublicFiles()
    {
        var storageProvider = new FakeFileStorageProvider(FileProvider.AmazonS3);
        var (service, fileRepository, _, _, _, _) = CreateSut(storageProvider);
        var file = CreateStagedFile(storageProvider.Provider);
        file.IsPrivate = false;
        await fileRepository.AddAsync(file);

        var url = await service.GetDownloadUrlAsync(file);

        Assert.Equal(new Uri("https://example.com/download"), url);
        Assert.False(storageProvider.LastDownloadIsPrivate);
        Assert.Null(storageProvider.LastDownloadExpiresIn);
    }

    [Fact]
    public async Task GetDownloadUrlAsync_UsesSignedUrlForPrivateFiles()
    {
        var storageProvider = new FakeFileStorageProvider(FileProvider.AmazonS3);
        var (service, fileRepository, _, _, _, _) = CreateSut(storageProvider, stagingTtlMinutes: 99, privateDownloadTtlMinutes: 45);
        var file = CreateStagedFile(storageProvider.Provider);
        file.IsPrivate = true;
        await fileRepository.AddAsync(file);

        var url = await service.GetDownloadUrlAsync(file);

        Assert.Equal(new Uri("https://example.com/download"), url);
        Assert.True(storageProvider.LastDownloadIsPrivate);
        Assert.Equal(TimeSpan.FromMinutes(45), storageProvider.LastDownloadExpiresIn);
    }

    private static FileRecord CreateStagedFile(FileProvider provider)
    {
        var now = DateTimeOffset.UtcNow;

        return new FileRecord
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Purpose = "license",
            EntityType = "pharmacy",
            EntityId = Guid.Parse("9f2c0af0-2b1c-44b3-b0c1-8d2e86d8b8cc"),
            Provider = provider,
            ProviderKey = "staging/license.pdf",
            StorageRegion = string.Empty,
            OriginalFilename = "license.pdf",
            MimeType = "application/pdf",
            Size = 123,
            Checksum = "checksum-123",
            Status = FileStatus.Staged,
            StagingExpiresAt = now.AddMinutes(30),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static (
        FileWorkflowService Service,
        FakeFileRepository FileRepository,
        FakeFileEventRepository FileEventRepository,
        FakeFileCleanupLogRepository CleanupLogRepository,
        FakeUnitOfWork UnitOfWork,
        FakeFileStorageProviderResolver Resolver) CreateSut(
        FakeFileStorageProvider storageProvider,
        int stagingTtlMinutes = 15,
        int privateDownloadTtlMinutes = 15)
    {
        var fileRepository = new FakeFileRepository();
        var fileEventRepository = new FakeFileEventRepository();
        var cleanupLogRepository = new FakeFileCleanupLogRepository();
        var unitOfWork = new FakeUnitOfWork();
        var resolver = new FakeFileStorageProviderResolver(storageProvider);
        var stagingOptions = Options.Create(new StagingOptions { TtlMinutes = stagingTtlMinutes });
        var privateDownloadLinkOptions = Options.Create(new PrivateDownloadLinkOptions
        {
            PrivateDownloadUrlTtlMinutes = privateDownloadTtlMinutes
        });

        var service = new FileWorkflowService(
            fileRepository,
            fileEventRepository,
            cleanupLogRepository,
            unitOfWork,
            resolver,
            stagingOptions,
            privateDownloadLinkOptions);

        return (service, fileRepository, fileEventRepository, cleanupLogRepository, unitOfWork, resolver);
    }

    private static string InvokeBuildCommitDestinationFolder(FileRecord file, string? destinationFolder, bool isPrivate)
    {
        var method = typeof(FileWorkflowService).GetMethod(
            "BuildCommitDestinationFolder",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildCommitDestinationFolder was not found.");

        return (string)method.Invoke(null, new object?[] { file, destinationFolder, isPrivate })!;
    }

    private sealed class FakeFileRepository : IFileRepository
    {
        private readonly Dictionary<Guid, FileRecord> _files = new();

        public List<FileRecord> AddedFiles { get; } = [];

        public int UpdateCallCount { get; private set; }

        public Task<FileRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_files.TryGetValue(id, out var file) ? file : null);
        }

        public Task<IReadOnlyList<FileRecord>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idSet = ids.ToHashSet();
            IReadOnlyList<FileRecord> files = _files.Values.Where(file => idSet.Contains(file.Id)).ToList();
            return Task.FromResult(files);
        }

        public Task AddAsync(FileRecord file, CancellationToken cancellationToken = default)
        {
            _files[file.Id] = file;
            AddedFiles.Add(file);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FileRecord file, CancellationToken cancellationToken = default)
        {
            _files[file.Id] = file;
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_files.ContainsKey(id));
        }

        public Task<IReadOnlyList<FileRecord>> GetExpiredStagedAsync(
            DateTimeOffset cutoff,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<FileRecord> files = [];
            return Task.FromResult(files);
        }
    }

    private sealed class FakeFileEventRepository : IFileEventRepository
    {
        public List<FileEvent> AddedEvents { get; } = [];

        public Task AddAsync(FileEvent fileEvent, CancellationToken cancellationToken = default)
        {
            AddedEvents.Add(fileEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileCleanupLogRepository : IFileCleanupLogRepository
    {
        public List<FileCleanupLog> AddedLogs { get; } = [];

        public Task AddAsync(FileCleanupLog log, CancellationToken cancellationToken = default)
        {
            AddedLogs.Add(log);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(0);
        }
    }

    private sealed class FakeFileStorageProviderResolver : IFileStorageProviderResolver
    {
        private readonly FakeFileStorageProvider _storageProvider;

        public FakeFileStorageProviderResolver(FakeFileStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
        }

        public int ResolveCallCount { get; private set; }

        public int ResolveDefaultCallCount { get; private set; }

        public IFileStorageProvider Resolve(FileProvider provider)
        {
            ResolveCallCount++;
            return _storageProvider;
        }

        public IFileStorageProvider ResolveDefault()
        {
            ResolveDefaultCallCount++;
            return _storageProvider;
        }
    }

    private sealed class FakeFileStorageProvider : IFileStorageProvider
    {
        public FakeFileStorageProvider(FileProvider provider)
        {
            Provider = provider;
        }

        public FileProvider Provider { get; }

        public string? LastUploadFileName { get; private set; }

        public string? LastUploadContentType { get; private set; }

        public string? LastPromoteStagedProviderKey { get; private set; }

        public string? LastPromoteDestinationFolder { get; private set; }

        public bool LastDownloadIsPrivate { get; private set; }

        public TimeSpan? LastDownloadExpiresIn { get; private set; }

        public int DeleteCallCount { get; private set; }

        public Task<FileStorageUploadResult> UploadAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            LastUploadFileName = fileName;
            LastUploadContentType = contentType;

            return Task.FromResult(new FileStorageUploadResult(
                $"staging/{Path.GetFileName(fileName)}",
                "region-1",
                "checksum-123",
                123,
                contentType));
        }

        public Task<FileStoragePromotionResult> PromoteAsync(
            string stagedProviderKey,
            string? destinationFolder,
            CancellationToken cancellationToken = default)
        {
            LastPromoteStagedProviderKey = stagedProviderKey;
            LastPromoteDestinationFolder = destinationFolder;

            return Task.FromResult(new FileStoragePromotionResult(
                $"{destinationFolder}/promoted-object",
                "region-2"));
        }

        public Task DeleteAsync(string providerKey, CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;
            return Task.CompletedTask;
        }

        public Task<Uri> GetDownloadUrlAsync(
            string providerKey,
            bool isPrivate,
            TimeSpan? expiresIn = null,
            CancellationToken cancellationToken = default)
        {
            LastDownloadIsPrivate = isPrivate;
            LastDownloadExpiresIn = expiresIn;
            return Task.FromResult(new Uri("https://example.com/download"));
        }
    }
}
