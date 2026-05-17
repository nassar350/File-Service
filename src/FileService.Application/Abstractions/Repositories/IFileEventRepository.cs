using FileService.Domain.Files;

namespace FileService.Application.Abstractions.Repositories;

public interface IFileEventRepository
{
    Task AddAsync(FileEvent fileEvent, CancellationToken cancellationToken = default);
}
