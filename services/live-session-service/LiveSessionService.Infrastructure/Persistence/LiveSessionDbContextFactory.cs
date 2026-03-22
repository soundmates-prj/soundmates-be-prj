using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LiveSessionService.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Uses environment variables or defaults for local development.
/// Priority: Command-line env vars > .env file > defaults
/// </summary>
public class LiveSessionDbContextFactory : IDesignTimeDbContextFactory<LiveSessionDbContext>
{
    public LiveSessionDbContext CreateDbContext(string[] args)
    {
        var connectionString = BuildConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<LiveSessionDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new LiveSessionDbContext(optionsBuilder.Options);
    }

    private static string BuildConnectionString()
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(cs))
        {
            return cs;
        }

        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? Environment.GetEnvironmentVariable("POSTGRES_PORT");
        var database = Environment.GetEnvironmentVariable("LIVE_SESSION_DB_NAME") ?? Environment.GetEnvironmentVariable("DB_NAME") ?? Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
        var username = Environment.GetEnvironmentVariable("DB_USER") ?? Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(port) ||
            string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Database configuration is missing. Set ConnectionStrings__DefaultConnection or DB_HOST/DB_PORT/LIVE_SESSION_DB_NAME/DB_USER/DB_PASSWORD.");
        }

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
    }
}
