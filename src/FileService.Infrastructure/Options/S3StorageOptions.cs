namespace FileService.Infrastructure.Options;

public sealed class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    public string BucketName { get; set; } = string.Empty;

    public string? Region { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string? ServiceUrl { get; set; }

    public bool ForcePathStyle { get; set; }

    public string StagingFolder { get; set; } = "staging";

    public string PublicFolder { get; set; } = "files";
}
