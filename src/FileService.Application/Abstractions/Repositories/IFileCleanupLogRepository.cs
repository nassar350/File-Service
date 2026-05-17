using FileService.Domain.Files;

namespace FileService.Application.Abstractions.Repositories;

public interface IFileCleanupLogRepository
{
    Task AddAsync(FileCleanupLog log, CancellationToken cancellationToken = default);
}
