using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LiveSessionService.Api.Extensions;

/// <summary>
/// Extension methods for database lifecycle management
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies any pending EF Core migrations automatically on startup.
    /// Safe to call every run — skips migrations that are already applied.
    /// Falls back gracefully when snapshot/model mismatch prevents MigrateAsync.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LiveSessionDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LiveSessionDbContext>>();

        try
        {
            var pending = await db.Database.GetPendingMigrationsAsync();
            var pendingList = pending.ToList();

            if (pendingList.Count == 0)
            {
                logger.LogInformation("[Migration] Database is up-to-date. No pending migrations.");
                return;
            }

            logger.LogInformation("[Migration] Applying {Count} pending migration(s): {Migrations}",
                pendingList.Count, string.Join(", ", pendingList));

            await db.Database.MigrateAsync(cancellationToken: default);

            logger.LogInformation("[Migration] All migrations applied successfully.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("pending changes") || ex.Message.Contains("PendingModelChangesWarning"))
        {
            // Model snapshot mismatch (e.g. multiple SessionSchedule definitions).
            // Check if the critical columns already exist — if so, we can safely start.
            logger.LogWarning("[Migration] Model snapshot mismatch detected. Verifying schema...");
            try
            {
                var conn = (NpgsqlConnection)db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    "SELECT 1 FROM information_schema.columns WHERE table_name='session_schedules' AND column_name='created_at'",
                    conn);
                var exists = await cmd.ExecuteScalarAsync() != null;

                if (exists)
                {
                    logger.LogInformation(
                        "[Migration] 'created_at' column confirmed in DB. " +
                        "Manual migration was applied. Service will start normally.");
                }
                else
                {
                    logger.LogError(
                        "[Migration] 'created_at' column is MISSING in DB! " +
                        "Apply manually: ALTER TABLE session_schedules ADD COLUMN created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW();");
                }
            }
            catch (Exception verifyEx)
            {
                logger.LogError(verifyEx, "[Migration] Schema verification failed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Migration] Failed to apply migrations. The app will still start — check DB connection.");
        }
    }
}
