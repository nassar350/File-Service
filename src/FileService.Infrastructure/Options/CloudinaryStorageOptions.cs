namespace FileService.Infrastructure.Options;

public sealed class CloudinaryStorageOptions
{
    public const string SectionName = "Storage:Cloudinary";

    public string CloudName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string? UploadPreset { get; set; }

    public string StagingFolder { get; set; } = "staging";

    public string PublicFolder { get; set; } = "files";
}
