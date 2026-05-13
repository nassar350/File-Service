using System.Text.Json;
using FileService.Domain.Enums;

namespace FileService.Application.Models;

public sealed record StageUploadRequest(
    Guid TenantId,
    Guid OwnerId,
    string Purpose,
    string EntityType,
    Guid EntityId,
    string FileName,
    string MimeType,
    Stream Content,
    JsonDocument? Metadata = null,
    FileProvider? Provider = null,
    int? StagingTtlMinutes = null);

public sealed record CommitUploadRequest(
    Guid FileId,
    string? DestinationFolder = null,
    Guid? ReplacesFileId = null);

public sealed record ReplaceUploadRequest(
    Guid FileId,
    string FileName,
    string MimeType,
    Stream Content,
    JsonDocument? Metadata = null);

public sealed record DeleteFileRequest(Guid FileId);
