using FileService.Domain.Enums;

namespace FileService.Application.Abstractions.Storage;

public interface IFileStorageProvider
{
    FileProvider Provider { get; }

    Task<FileStorageUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<FileStoragePromotionResult> PromoteAsync(
        string stagedProviderKey,
        string? destinationFolder,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string providerKey, CancellationToken cancellationToken = default);

    Task<Uri> GetDownloadUrlAsync(
        string providerKey,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default);
}

public sealed record FileStorageUploadResult(
    string ProviderKey,
    string? StorageRegion,
    string? Checksum,
    long Size,
    string? MimeType);

public sealed record FileStoragePromotionResult(
    string ProviderKey,
    string? StorageRegion);
