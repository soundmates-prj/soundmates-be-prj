using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AuthService.Infrastructure.Persistence
{
    /// <summary>
    /// Design-time factory for EF Core migrations.
    /// Uses environment variables or defaults for local development.
    /// Priority: Command-line env vars > .env file > defaults
    /// </summary>
    public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
    {
        public AuthDbContext CreateDbContext(string[] args)
        {
            // Save existing environment variables (set from command line)
            var existingVars = new Dictionary<string, string>();
            foreach (var key in new[] { "POSTGRES_HOST", "POSTGRES_PORT", "POSTGRES_DATABASE", "POSTGRES_USERNAME", "POSTGRES_PASSWORD" })
            {
                var value = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(value))
                {
                    existingVars[key] = value;
                }
            }

            // Try to load .env file for design-time (only for vars not already set)
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
                            // Only set if not already set from command line
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
                // Ignore - will use defaults
            }

            // Restore command-line environment variables (highest priority)
            foreach (var kvp in existingVars)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }

            var connectionString = BuildConnectionString();

            var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AuthDbContext(optionsBuilder.Options);
        }

        private static string BuildConnectionString()
        {
            // Get values from environment variables or use defaults
            var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ?? "auth_db";
            var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME") ?? "postgres";
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
        }
    }
}
