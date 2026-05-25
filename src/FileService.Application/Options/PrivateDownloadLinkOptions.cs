namespace FileService.Application.Options;

public sealed class PrivateDownloadLinkOptions
{
    public const string SectionName = "Storage:S3";

    public int PrivateDownloadUrlTtlMinutes { get; set; } = 15;
}