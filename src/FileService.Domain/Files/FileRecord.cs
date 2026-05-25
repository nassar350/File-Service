using System.Text.Json;
using FileService.Domain.Enums;

namespace FileService.Domain.Files;

public sealed class FileRecord
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OwnerId { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public bool IsPrivate { get; set; }

    public FileProvider Provider { get; set; }

    public string ProviderKey { get; set; } = string.Empty;

    public string StorageRegion { get; set; } = string.Empty;

    public string OriginalFilename { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long Size { get; set; }

    public string Checksum { get; set; } = string.Empty;

    public JsonDocument? Metadata { get; set; }

    public FileStatus Status { get; set; }

    public DateTimeOffset? StagingExpiresAt { get; set; }

    public DateTimeOffset? CommittedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<FileEvent> Events { get; set; } = new List<FileEvent>();

    public ICollection<FileCleanupLog> CleanupLogs { get; set; } = new List<FileCleanupLog>();
}