using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LiveSessionService.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Uses environment variables and root .env for local development.
/// </summary>
public class LiveSessionDbContextFactory : IDesignTimeDbContextFactory<LiveSessionDbContext>
{
    public LiveSessionDbContext CreateDbContext(string[] args)
    {
        TryLoadRootEnvFile();

        var connectionString = BuildConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<LiveSessionDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new LiveSessionDbContext(optionsBuilder.Options);
    }

    private static void TryLoadRootEnvFile()
    {
        var existingVars = CaptureExistingEnv();

        try
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../..", ".env")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../.env")),
                Path.Combine(AppContext.BaseDirectory, ".env")
            };

            var envPath = candidates.FirstOrDefault(File.Exists);
            if (envPath is null)
            {
                return;
            }

            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                // Keep command-line exported values as highest priority.
                if (!existingVars.ContainsKey(key))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
        catch
        {
            // Continue with already-exported environment variables.
        }
        finally
        {
            foreach (var kvp in existingVars)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }
    }

    private static Dictionary<string, string> CaptureExistingEnv()
    {
        var keys = new[]
        {
            "ConnectionStrings__DefaultConnection",
            "DB_HOST",
            "DB_PORT",
            "LIVE_SESSION_DB_NAME",
            "DB_NAME",
            "DB_USER",
            "DB_PASSWORD",
            "POSTGRES_HOST",
            "POSTGRES_PORT",
            "POSTGRES_DATABASE",
            "POSTGRES_USERNAME",
            "POSTGRES_PASSWORD"
        };

        var existing = new Dictionary<string, string>();
        foreach (var key in keys)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                existing[key] = value;
            }
        }

        return existing;
    }

    private static string BuildConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? Environment.GetEnvironmentVariable("POSTGRES_PORT");
        var database = Environment.GetEnvironmentVariable("LIVE_SESSION_DB_NAME")
            ?? Environment.GetEnvironmentVariable("DB_NAME")
            ?? Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
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
