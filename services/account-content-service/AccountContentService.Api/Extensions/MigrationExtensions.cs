using AccountContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountContentService.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountContentDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database migration...");

        const int maxRetries = 5;
        const int baseDelaySeconds = 2;

        for (var retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                if (retry > 0)
                {
                    var delaySeconds = baseDelaySeconds * (int)Math.Pow(2, retry - 1);
                    logger.LogWarning(
                        "Retrying database migration ({Retry}/{MaxRetries}) in {DelaySeconds} seconds...",
                        retry,
                        maxRetries,
                        delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), CancellationToken.None);
                }

                var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
                if (pendingMigrations.Count == 0)
                {
                    logger.LogInformation("No pending migrations found.");
                }
                else
                {
                    logger.LogInformation("Pending migrations: {Migrations}", string.Join(", ", pendingMigrations));
                }

                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully.");

                try
                {
                    await DataSeeder.SeedAsync(dbContext);
                    logger.LogInformation("Database seeding completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Database seeding failed. Application will continue to run.");
                }

                return;
            }
            catch (Exception ex)
            {
                if (retry == maxRetries - 1)
                {
                    logger.LogCritical(ex, "Database migration failed after {MaxRetries} attempts.", maxRetries);
                    throw;
                }

                logger.LogWarning(ex, "Database migration attempt {Attempt} failed.", retry + 1);
            }
        }
    }
}
