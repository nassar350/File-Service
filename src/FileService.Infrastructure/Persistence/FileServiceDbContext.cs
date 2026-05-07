using FileService.Domain.Files;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Persistence;

public sealed class FileServiceDbContext(DbContextOptions<FileServiceDbContext> options) : DbContext(options)
{
    public DbSet<FileRecord> Files => Set<FileRecord>();

    public DbSet<FileEvent> FileEvents => Set<FileEvent>();

    public DbSet<FileCleanupLog> FileCleanupLogs => Set<FileCleanupLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileServiceDbContext).Assembly);
    }
}