using FileService.Domain.Enums;
using FileService.Domain.Files;

namespace FileService.Application.Models;

public sealed record FileWorkflowResponse(
    Guid Id,
    string Purpose,
    string EntityType,
    Guid EntityId,
    FileProvider Provider,
    string ProviderKey,
    string StorageRegion,
    string OriginalFilename,
    string MimeType,
    long Size,
    string Checksum,
    FileStatus Status,
    DateTimeOffset? StagingExpiresAt,
    DateTimeOffset? CommittedAt,
    DateTimeOffset? DeletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static FileWorkflowResponse From(FileRecord file)
    {
        return new FileWorkflowResponse(
            file.Id,
            file.Purpose,
            file.EntityType,
            file.EntityId,
            file.Provider,
            file.ProviderKey,
            file.StorageRegion,
            file.OriginalFilename,
            file.MimeType,
            file.Size,
            file.Checksum,
            file.Status,
            file.StagingExpiresAt,
            file.CommittedAt,
            file.DeletedAt,
            file.CreatedAt,
            file.UpdatedAt);
    }
}