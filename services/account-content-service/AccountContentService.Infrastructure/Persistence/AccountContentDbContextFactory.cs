using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class AccountContentDbContextFactory
    : IDesignTimeDbContextFactory<AccountContentDbContext>
{
    public AccountContentDbContext CreateDbContext(string[] args)
    {
        try
        {
            var envCandidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../..", ".env"))
            };

            var envFile = envCandidates.FirstOrDefault(File.Exists);
            if (envFile is not null)
            {
                Env.Load(envFile);
            }
        }
        catch
        {
        }

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["ConnectionStrings__DefaultConnection"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var host = configuration["DB_HOST"] ?? configuration["POSTGRES_HOST"];
            var port = configuration["DB_PORT"] ?? configuration["POSTGRES_PORT"];
            var database = configuration["ACCOUNT_CONTENT_DB_NAME"] ?? configuration["DB_NAME"] ?? configuration["POSTGRES_DATABASE"];
            var username = configuration["DB_USER"] ?? configuration["POSTGRES_USERNAME"];
            var password = configuration["DB_PASSWORD"] ?? configuration["POSTGRES_PASSWORD"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(port) ||
                string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Database configuration is missing. Set ConnectionStrings__DefaultConnection or DB_HOST/DB_PORT/ACCOUNT_CONTENT_DB_NAME/DB_USER/DB_PASSWORD.");
            }

            connectionString =
                $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
        }

        var optionsBuilder = new DbContextOptionsBuilder<AccountContentDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new AccountContentDbContext(optionsBuilder.Options);
    }
}