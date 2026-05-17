using FileService.Domain.Enums;

namespace FileService.Application.Abstractions.Storage;

public interface IFileStorageProviderSelector
{
    FileProvider Select(FileStorageRequest request);
}

public sealed record FileStorageRequest(
    Guid TenantId,
    string Purpose,
    string MimeType,
    long Size,
    string? Region,
    FileProvider? PreferredProvider = null);
