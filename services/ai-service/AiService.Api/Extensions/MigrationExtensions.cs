using AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.RegularExpressions;

namespace AiService.Api.Extensions;

/// <summary>
/// Extension methods for database migration and seeding
/// Handles automatic database migrations with retry logic
/// </summary>
public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Ensure ai_db exists before migration
        await EnsureDatabaseExistsAsync("ai_db", logger);

        logger.LogInformation("Starting database migration...");

        const int maxRetries = 5;
        const int baseDelaySeconds = 2;
        var retry = 0;

        while (true)
        {
            try
            {
                if (retry > 0)
                {
                    var delay = baseDelaySeconds * (int)Math.Pow(2, retry - 1);
                    await Task.Delay(TimeSpan.FromSeconds(delay), CancellationToken.None);
                }

                var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                var appliedMigrations = db.Database.GetAppliedMigrations().ToList();

                logger.LogInformation(
                    "Applied migrations: {Count} - {Migrations}",
                    appliedMigrations.Count,
                    string.Join(", ", appliedMigrations));
                logger.LogInformation(
                    "Pending migrations: {Count} - {Migrations}",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));

                await db.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully.");
                return;
            }
            catch (Exception ex)
            {
                retry++;
                if (retry >= maxRetries)
                {
                    logger.LogCritical(ex, "Database migration failed after retries.");
                    throw;
                }
                logger.LogWarning(ex, "Migration attempt {Retry} failed, retrying...", retry);
            }
        }
    }

    public static async Task SeedDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var existingVoices = await db.TtsVoices.ToListAsync();
            logger.LogInformation("Found {Count} voices in database.", existingVoices.Count);

            var defaultVoices = new List<AiService.Domain.Entities.TtsVoice>
            {
                // Baseline & Existing models
                new() { VoiceId = Guid.NewGuid(), Provider = "vieneutts", VoiceCode = "q4",
                    DisplayName = "VieNeu Fast (Q4)", Region = "VN", Gender = "Unknown",
                    Model = "q4", IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { VoiceId = Guid.NewGuid(), Provider = "vieneutts", VoiceCode = "q8",
                    DisplayName = "VieNeu High Quality (Q8)", Region = "VN", Gender = "Unknown",
                    Model = "q8", IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { VoiceId = Guid.NewGuid(), Provider = "vieneutts", VoiceCode = "ngochuyen",
                    DisplayName = "Ngọc Huyền (Standard)", Region = "VN", Gender = "Female",
                    Model = "ngochuyen", IsActive = true, CreatedAt = DateTime.UtcNow },

                // Additional voices from VieNeu-TTS reference
                new() { VoiceId = Guid.NewGuid(), Provider = "vieneutts", VoiceCode = "binh",
                    DisplayName = "Bình (Male North)", Region = "North", Gender = "Male",
                    Model = "binh", IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { VoiceId = Guid.NewGuid(), Provider = "vieneutts", VoiceCode = "tuyen",
                    DisplayName = "Tuyên (Male North)", Region = "North", Gender = "Male",
                    Model = "tuyen", IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { VoiceId = Guid.NewGuid(), Provider = "vieneutts", VoiceCode = "vinh",
                    DisplayName = "Vinh (Male South)", Region = "South", Gender = "Male",
                    Model = "vinh", IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { VoiceId = Guid.NewGuid(), Provider = "vieneutts", VoiceCode = "ly",
                    DisplayName = "Ly (Female North)", Region = "North", Gender = "Female",
                    Model = "ly", IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { VoiceId = Guid.NewGuid(), Provider = "vieneutts", VoiceCode = "ngoc",
                    DisplayName = "Ngọc (Female North)", Region = "North", Gender = "Female",
                    Model = "ngoc", IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { VoiceId = Guid.NewGuid(), Provider = "vieneutts", VoiceCode = "doan",
                    DisplayName = "Đoan (Female South)", Region = "South", Gender = "Female",
                    Model = "doan", IsActive = true, CreatedAt = DateTime.UtcNow },
            };

            var addedCount = 0;
            foreach (var v in defaultVoices)
            {
                if (!existingVoices.Any(ev => ev.VoiceCode == v.VoiceCode && ev.Provider == v.Provider))
                {
                    db.TtsVoices.Add(v);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Seeding completed. Added {Count} new default voices.", addedCount);
            }
            else
            {
                logger.LogInformation("All default voices already exist. No seeding needed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed default voices.");
        }
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
}
