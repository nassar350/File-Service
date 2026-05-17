namespace FileService.Application.Options;

public sealed class StagingOptions
{
    public const string SectionName = "Staging";

    public int TtlMinutes { get; set; } = 15;
}
