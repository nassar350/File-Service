using Microsoft.Extensions.Configuration;

namespace FileService.Infrastructure.Persistence;

internal static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration? configuration = null)
    {
        var connectionString =
            configuration?.GetConnectionString("DefaultConnection")
            ?? configuration?["ConnectionStrings:DefaultConnection"]
            ?? configuration?["ConnectionStrings__DefaultConnection"]
            ?? configuration?["Postgres_ConnectionString"]
            ?? configuration?["Postgres:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection")
            ?? Environment.GetEnvironmentVariable("Postgres_ConnectionString")
            ?? Environment.GetEnvironmentVariable("Postgres:ConnectionString")
            ?? throw new InvalidOperationException("Missing connection string. Set ConnectionStrings__DefaultConnection or Postgres_ConnectionString in .env or appsettings.");

        return connectionString;
    }
}