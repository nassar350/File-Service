using FileService.Application.Abstractions.Repositories;
using FileService.Domain.Files;

namespace FileService.Infrastructure.Persistence;

public sealed class FileCleanupLogRepository : IFileCleanupLogRepository
{
    private readonly FileServiceDbContext _dbContext;

    public FileCleanupLogRepository(FileServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(FileCleanupLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.FileCleanupLogs.AddAsync(log, cancellationToken);
    }
}
