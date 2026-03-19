using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiService.Infrastructure.Persistence;

public class AiDbContextFactory : IDesignTimeDbContextFactory<AiDbContext>
{
    public AiDbContext CreateDbContext(string[] args)
    {
        var existingVars = new Dictionary<string, string>();
        foreach (var key in new[] { "POSTGRES_HOST", "POSTGRES_PORT", "POSTGRES_DATABASE", "POSTGRES_USERNAME", "POSTGRES_PASSWORD" })
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(value))
            {
                existingVars[key] = value;
            }
        }

        try
        {
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (!File.Exists(envPath))
            {
                envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
            }

            if (File.Exists(envPath))
            {
                foreach (var line in File.ReadAllLines(envPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        if (!existingVars.ContainsKey(key))
                        {
                            Environment.SetEnvironmentVariable(key, parts[1].Trim());
                        }
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        foreach (var kvp in existingVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        var connectionString = BuildConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<AiDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AiDbContext(optionsBuilder.Options);
    }

    private static string BuildConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ?? "ai_db";
        var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
    }
}

