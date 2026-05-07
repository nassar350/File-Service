using FileService.Domain.Enums;

namespace FileService.Domain.Files;

public sealed class FileCleanupLog
{
    public Guid Id { get; set; }

    public Guid FileId { get; set; }

    public FileRecord File { get; set; } = null!;

    public string Action { get; set; } = string.Empty;

    public FileCleanupLogStatus Status { get; set; }

    public string? Message { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}