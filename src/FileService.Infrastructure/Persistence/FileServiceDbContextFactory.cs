using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FileService.Infrastructure.Persistence;

public sealed class FileServiceDbContextFactory : IDesignTimeDbContextFactory<FileServiceDbContext>
{
    public FileServiceDbContext CreateDbContext(string[] args)
    {
        Env.TraversePath().Load();

        var optionsBuilder = new DbContextOptionsBuilder<FileServiceDbContext>();
        var connectionString = ConnectionStringResolver.Resolve();

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions.MigrationsAssembly(typeof(FileServiceDbContext).Assembly.FullName));

        return new FileServiceDbContext(optionsBuilder.Options);
    }
}