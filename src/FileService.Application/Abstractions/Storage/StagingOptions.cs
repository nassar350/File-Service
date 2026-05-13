namespace FileService.Application.Abstractions.Storage;

public sealed class StagingOptions
{
    public const string SectionName = "Staging";

    public int TtlMinutes { get; set; } = 15;
}
