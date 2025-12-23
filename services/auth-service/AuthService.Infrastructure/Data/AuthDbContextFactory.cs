using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace AuthService.Infrastructure
{
    public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
    {
        public AuthDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            var connectionString = ResolveConnectionString(configuration);

            var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AuthDbContext(optionsBuilder.Options);
        }

        private static string ResolveConnectionString(IConfiguration configuration)
        {
            var raw = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
            }

            return Regex.Replace(raw, "\\$\\{(?<key>[A-Za-z0-9_]+)\\}", match =>
            {
                var key = match.Groups["key"].Value;
                var value = Environment.GetEnvironmentVariable(key) ?? configuration[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException($"Missing environment variable '{key}' required for connection string.");
                }
                return value;
            });
        }
    }
}
