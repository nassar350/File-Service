using FileService.Application.Abstractions.Repositories;

namespace FileService.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FileServiceDbContext _dbContext;

    public UnitOfWork(FileServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
