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
            Env.Load();
        }
        catch
        {
        }

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var host = configuration["POSTGRES_HOST"];
        var port = configuration["POSTGRES_PORT"];
        var database = configuration["POSTGRES_DATABASE"];
        var username = configuration["POSTGRES_USERNAME"];
        var password = configuration["POSTGRES_PASSWORD"];

        var connectionString =
            $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AccountContentDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new AccountContentDbContext(optionsBuilder.Options);
    }
}