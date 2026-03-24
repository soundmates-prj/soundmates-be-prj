using AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
                new AiService.Domain.Entities.TtsVoice {
                    VoiceId = Guid.NewGuid(),
                    Provider = "vieneutts",
                    VoiceCode = "ngochuyen",
                    DisplayName = "Ngọc Huyền (Standard)",
                    Region = "VN",
                    Gender = "Female",
                    Model = "ngochuyen",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new AiService.Domain.Entities.TtsVoice {
                    VoiceId = Guid.NewGuid(),
                    Provider = "vieneutts",
                    VoiceCode = "q4",
                    DisplayName = "VieNeu Fast (Q4)",
                    Region = "VN",
                    Gender = "Unknown",
                    Model = "q4",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new AiService.Domain.Entities.TtsVoice {
                    VoiceId = Guid.NewGuid(),
                    Provider = "vieneutts",
                    VoiceCode = "q8",
                    DisplayName = "VieNeu High Quality (Q8)",
                    Region = "VN",
                    Gender = "Unknown",
                    Model = "q8",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
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
}

