using AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiService.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

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

    public static Task SeedDataAsync(this WebApplication app)
    {
        // optional: seed default voices
        return Task.CompletedTask;
    }
}

