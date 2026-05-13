using FileService.Application.Abstractions.Repositories;
using FileService.Domain.Files;
using FileService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Persistence;

public sealed class FileRepository : IFileRepository
{
    private readonly FileServiceDbContext _dbContext;

    public FileRepository(FileServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FileRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(file => file.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FileRecord>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Files
            .AsNoTracking()
            .Where(file => ids.Contains(file.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FileRecord file, CancellationToken cancellationToken = default)
    {
        await _dbContext.Files.AddAsync(file, cancellationToken);
    }

    public Task UpdateAsync(FileRecord file, CancellationToken cancellationToken = default)
    {
        _dbContext.Files.Update(file);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Files.AnyAsync(file => file.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FileRecord>> GetExpiredStagedAsync(
        DateTimeOffset cutoff,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Files
            .AsNoTracking()
            .Where(file => file.Status == FileStatus.Staged
                && file.StagingExpiresAt.HasValue
                && file.StagingExpiresAt <= cutoff)
            .OrderBy(file => file.StagingExpiresAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
