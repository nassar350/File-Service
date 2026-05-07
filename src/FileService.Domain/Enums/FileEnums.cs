namespace FileService.Domain.Enums;

public enum FileProvider
{
    Unknown = 0,
    Cloudinary = 1,
    AmazonS3 = 2,
    Local = 3
}

public enum FileEventType
{
    Unknown = 0,
    Staged = 1,
    Committed = 2,
    Replaced = 3,
    Aborted = 4,
    Deleted = 5,
    MetadataUpdated = 6
}

public enum FileCleanupLogStatus
{
    Unknown = 0,
    Pending = 1,
    InProgress = 2,
    Succeeded = 3,
    Failed = 4,
    Skipped = 5
}

public enum FileStatus
{
    Unknown = 0,
    Staged = 1,
    Committed = 2,
    Deleted = 3,
    Failed = 4
}