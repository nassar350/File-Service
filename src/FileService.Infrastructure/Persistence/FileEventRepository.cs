using FileService.Application.Abstractions.Repositories;
using FileService.Domain.Files;

namespace FileService.Infrastructure.Persistence;

public sealed class FileEventRepository : IFileEventRepository
{
    private readonly FileServiceDbContext _dbContext;

    public FileEventRepository(FileServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(FileEvent fileEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.FileEvents.AddAsync(fileEvent, cancellationToken);
    }
}
