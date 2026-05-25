using FileService.Application.Models;
using FileService.Domain.Files;

namespace FileService.Application.Abstractions.Services;

public interface IFileWorkflowService
{
    Task<FileRecord> StageUploadAsync(StageUploadRequest request, CancellationToken cancellationToken = default);

    Task<FileRecord> CommitUploadAsync(CommitUploadRequest request, CancellationToken cancellationToken = default);

    Task<FileRecord> ReplaceUploadAsync(ReplaceUploadRequest request, CancellationToken cancellationToken = default);

    Task AbortUploadAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(DeleteFileRequest request, CancellationToken cancellationToken = default);

    Task<FileRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Uri> GetDownloadUrlAsync(FileRecord file, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileRecord>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
