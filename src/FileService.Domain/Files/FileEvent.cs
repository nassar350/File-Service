using System.Text.Json;
using FileService.Domain.Enums;

namespace FileService.Domain.Files;

public sealed class FileEvent
{
    public Guid Id { get; set; }

    public Guid FileId { get; set; }

    public FileRecord File { get; set; } = null!;

    public FileEventType EventType { get; set; }

    public JsonDocument? Metadata { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}