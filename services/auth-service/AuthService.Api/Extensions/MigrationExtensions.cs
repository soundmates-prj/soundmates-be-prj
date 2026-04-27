using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.RegularExpressions;

namespace AuthService.Api.Extensions;

/// <summary>
/// Extension methods for database migration and seeding
/// Handles automatic database migrations with retry logic and default data seeding
/// </summary>
public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Ensure auth_db exists before migration (docker-compose only creates 'postgres' default DB)
        await EnsureDatabaseExistsAsync("auth_db", logger);

        logger.LogInformation("Starting database migration...");

        const int maxRetries = 5;
        const int baseDelaySeconds = 2;
        var retryCount = 0;
        var success = false;

        while (retryCount < maxRetries && !success)
        {
            try
            {
                if (retryCount > 0)
                {
                    var delaySeconds = baseDelaySeconds * (int)Math.Pow(2, retryCount - 1);
                    logger.LogInformation(
                        "Retry attempt {RetryCount}/{MaxRetries} after {DelaySeconds} seconds...",
                        retryCount, maxRetries, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), CancellationToken.None);
                }

                var canConnect = await dbContext.Database.CanConnectAsync();
                logger.LogInformation("Database connection check: {CanConnect}", canConnect);

                var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
                var appliedMigrations = dbContext.Database.GetAppliedMigrations().ToList();

                logger.LogInformation(
                    "Applied migrations: {Count} - {Migrations}",
                    appliedMigrations.Count,
                    string.Join(", ", appliedMigrations));
                logger.LogInformation(
                    "Pending migrations: {Count} - {Migrations}",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));

                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully.");

                var remainingPending = dbContext.Database.GetPendingMigrations().ToList();
                if (remainingPending.Any())
                    logger.LogWarning("Warning: Some migrations may not have been applied: {Migrations}",
                        string.Join(", ", remainingPending));
                else
                    logger.LogInformation("All migrations have been applied successfully.");

                success = true;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    logger.LogCritical(ex,
                        "CRITICAL: Database migration failed after {MaxRetries} attempts.",
                        maxRetries);
                    throw;
                }
                logger.LogWarning(ex,
                    "Migration attempt {RetryCount} failed: {Message}. Will retry...",
                    retryCount, ex.Message);
            }
        }

        if (!success)
            throw new InvalidOperationException(
                "Database migration failed. Please check the database connection and try again.");
    }

    /// <summary>
    /// Creates the database if it doesn't already exist.
    /// Enlist=false is required to avoid error 25001:
    /// "CREATE DATABASE cannot be executed from a function/transaction".
    /// </summary>
    private static async Task EnsureDatabaseExistsAsync(string dbName, ILogger logger)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST")
            ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT")
            ?? Environment.GetEnvironmentVariable("POSTGRES_PORT")
            ?? "5432";
        var user = Environment.GetEnvironmentVariable("DB_USER")
            ?? Environment.GetEnvironmentVariable("POSTGRES_USER")
            ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
            ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
            ?? "postgres";

        if (string.IsNullOrEmpty(host))
        {
            var fullConnString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrEmpty(fullConnString))
            {
                host = ExtractFromConnString(fullConnString, "Host") ?? "postgres";
                port = ExtractFromConnString(fullConnString, "Port") ?? "5432";
                user = ExtractFromConnString(fullConnString, "Username") ?? "postgres";
                password = ExtractFromConnString(fullConnString, "Password") ?? "postgres";
            }
            else
            {
                host = "localhost";
            }
        }

        var masterConnString =
            $"Host={host};Port={port};Database=postgres;Username={user};Password={password};" +
            $"Ssl Mode=Disable;Trust Server Certificate=True;Enlist=false";

        const int maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(masterConnString);
                await conn.OpenAsync();

                // Check if database already exists
                await using (var checkCmd = new NpgsqlCommand(
                    $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'", conn))
                {
                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists != null)
                    {
                        logger.LogInformation(
                            "Database '{DbName}' already exists on host '{Host}'.", dbName, host);
                        return;
                    }
                }

                // CREATE DATABASE cannot be inside a transaction block (Enlist=false makes this work)
                await using var createCmd = new NpgsqlCommand(
                    $@"CREATE DATABASE ""{dbName}""", conn);
                await createCmd.ExecuteNonQueryAsync();

                logger.LogInformation(
                    "Database '{DbName}' created successfully on host '{Host}'.", dbName, host);
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "42P04")
            {
                // 42P04 = duplicate_db — database was created concurrently by another process
                logger.LogInformation(
                    "Database '{DbName}' already exists (concurrent creation on host '{Host}').", dbName, host);
                return;
            }
            catch (NpgsqlException ex)
            {
                if (attempt == maxRetries)
                {
                    logger.LogCritical(ex,
                        "Could not create database '{DbName}' after {MaxRetries} attempts (host={Host}).",
                        dbName, maxRetries, host);
                    throw;
                }
                logger.LogWarning(ex,
                    "Could not create DB '{DbName}' (attempt {Attempt}/{MaxRetries}, host={Host}). Retrying in {Delay}s...",
                    dbName, attempt, maxRetries, host, retryDelay.TotalSeconds);
            }

            await Task.Delay(retryDelay);
            retryDelay *= 2;
        }
    }

    private static string? ExtractFromConnString(string connString, string key)
    {
        var match = Regex.Match(connString, $@"(?:^|;|\s){key}=([^;]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    public static async Task SeedDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            await SeedDefaultRolesAsync(scope, logger);
            logger.LogInformation("Waiting for roles to be synced to query service...");
            await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
            logger.LogInformation("Role seeding completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error during role seeding. Application will continue.");
        }
    }

    private static async Task SeedDefaultRolesAsync(AsyncServiceScope scope, ILogger logger)
    {
        try
        {
            var roleRepository = scope.ServiceProvider
                .GetRequiredService<AuthService.Domain.Interfaces.IRoleRepository>();
            var commandDispatcher = scope.ServiceProvider
                .GetRequiredService<AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces.ICommandDispatcher>();

            var defaultRoles = new[] { "MEMBER", "HOST", "STAFF", "ADMIN" };

            foreach (var roleName in defaultRoles)
            {
                var existingRole = await roleRepository.GetByNameAsync(roleName);
                if (existingRole == null)
                {
                    logger.LogInformation("Creating default role: {RoleName}", roleName);
                    var createRoleCmd = new AuthService.Application.Features.Role.Commands.CreateRoleCommand { Name = roleName };
                    var result = await commandDispatcher.Send<
                        AuthService.Application.Features.Role.Commands.CreateRoleCommand, Guid>(
                        createRoleCmd, CancellationToken.None);

                    if (result.IsSuccess && result.Data != Guid.Empty)
                        logger.LogInformation("Successfully created role: {RoleName} with ID: {RoleId}", roleName, result.Data);
                    else
                        logger.LogWarning("Failed to create role {RoleName}: {Error}", roleName, result.ErrorMessage ?? "Unknown error");
                }
                else
                {
                    logger.LogDebug("Role {RoleName} already exists", roleName);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding default roles. Continuing anyway...");
        }
    }
}
