using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Api.Extensions;

/// <summary>
/// Extension methods for database lifecycle management
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies any pending EF Core migrations automatically on startup.
    /// Safe to call every run — skips migrations that are already applied.
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

            await db.Database.MigrateAsync();

            logger.LogInformation("[Migration] All migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Migration] Failed to apply migrations. The app will still start — check DB connection and schema manually.");
        }
    }
}
