using FileService.Domain.Files;

namespace FileService.Application.Abstractions.Repositories;

public interface IFileRepository
{
    Task<FileRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileRecord>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task AddAsync(FileRecord file, CancellationToken cancellationToken = default);

    Task UpdateAsync(FileRecord file, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileRecord>> GetExpiredStagedAsync(
        DateTimeOffset cutoff,
        int batchSize,
        CancellationToken cancellationToken = default);
}
