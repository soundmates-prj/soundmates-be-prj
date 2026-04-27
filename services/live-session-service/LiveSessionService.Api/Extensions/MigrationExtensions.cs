using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.RegularExpressions;

namespace LiveSessionService.Api.Extensions;

/// <summary>
/// Extension methods for database migration and seeding
/// Handles automatic database migrations with retry logic
/// </summary>
public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveSessionDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Ensure live_session_db exists before migration
        await EnsureDatabaseExistsAsync(logger);

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

                // PendingModelChangesWarning is suppressed in AddDbContext (DependencyInjection)
                // so MigrateAsync will succeed even when snapshot is out of sync with the model.
                logger.LogInformation("Applying migrations...");
                await dbContext.Database.MigrateAsync();

                logger.LogInformation("Applying runtime schema updates for new columns...");
                await dbContext.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE ""live_session_chats"" ADD COLUMN IF NOT EXISTS ""avatar_url"" text;
                    ALTER TABLE ""live_session_chats"" ADD COLUMN IF NOT EXISTS ""user_name"" text;
                ");

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
    /// Creates the live_session_db database if it doesn't already exist.
    /// Enlist=false is critical to avoid error 25001:
    /// "CREATE DATABASE cannot be executed from a function/transaction".
    /// </summary>
    private static async Task EnsureDatabaseExistsAsync(ILogger logger)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST")
            ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT")
            ?? Environment.GetEnvironmentVariable("POSTGRES_PORT")
            ?? "5432";
        var user = Environment.GetEnvironmentVariable("DB_USER")
            ?? Environment.GetEnvironmentVariable("POSTGRES_USERNAME")
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

                await using (var checkCmd = new NpgsqlCommand(
                    "SELECT 1 FROM pg_database WHERE datname = 'live_session_db'", conn))
                {
                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists != null)
                    {
                        logger.LogInformation(
                            "Database 'live_session_db' already exists on host '{Host}'.", host);
                        return;
                    }
                }

                await using var createCmd = new NpgsqlCommand(
                    @"CREATE DATABASE ""live_session_db""", conn);
                await createCmd.ExecuteNonQueryAsync();

                logger.LogInformation(
                    "Database 'live_session_db' created successfully on host '{Host}'.", host);
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "42P04")
            {
                // 42P04 = duplicate_db — concurrent creation
                logger.LogInformation(
                    "Database 'live_session_db' already exists (concurrent creation on host '{Host}').", host);
                return;
            }
            catch (NpgsqlException ex)
            {
                if (attempt == maxRetries)
                {
                    logger.LogCritical(ex,
                        "Could not create database 'live_session_db' after {MaxRetries} attempts (host={Host}).",
                        maxRetries, host);
                    throw;
                }
                logger.LogWarning(ex,
                    "Could not create DB 'live_session_db' (attempt {Attempt}/{MaxRetries}, host={Host}). Retrying in {Delay}s...",
                    attempt, maxRetries, host, retryDelay.TotalSeconds);
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
}
