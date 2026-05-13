using FileService.Domain.Enums;

namespace FileService.Application.Abstractions.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public FileProvider DefaultProvider { get; set; } = FileProvider.Unknown;
}
