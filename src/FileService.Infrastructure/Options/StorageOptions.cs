using FileService.Domain.Enums;

namespace FileService.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public FileProvider DefaultProvider { get; set; } = FileProvider.Unknown;
}
