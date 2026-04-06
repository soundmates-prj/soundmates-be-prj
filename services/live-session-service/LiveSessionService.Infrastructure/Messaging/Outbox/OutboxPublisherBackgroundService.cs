using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Infrastructure.Messaging.Outbox;

public sealed class OutboxPublisherBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherBackgroundService> _logger;

    public OutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory, 
        ILogger<OutboxPublisherBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for app startup to complete
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        
        var delay = TimeSpan.FromSeconds(2);

        _logger.LogInformation("Outbox Publisher Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<LiveSessionDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IMessageBusPublisher>();

                var batch = await db.Set<OutboxMessage>()
                    .Where(x => x.ProcessedOnUtc == null)
                    .OrderBy(x => x.OccurredOnUtc)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var msg in batch)
                {
                    try
                    {
                        await publisher.PublishAsync(msg.Type, msg.Payload, stoppingToken);
                        msg.ProcessedOnUtc = DateTime.UtcNow;
                        msg.Error = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish message {MessageId} of type {MessageType}", 
                            msg.Id, msg.Type);
                        msg.Error = ex.Message;
                    }
                }

                if (batch.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Processed {Count} outbox messages", batch.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publisher loop failed");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}
