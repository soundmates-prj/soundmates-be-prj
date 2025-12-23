using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Messaging
{
    public sealed class OutboxPublisherBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisherBackgroundService> _logger;

        public OutboxPublisherBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for app startup to complete (give DB initialization time)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            
            var delay = TimeSpan.FromSeconds(2);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Use AsyncServiceScope for proper async disposal
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
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
                            _logger.LogWarning(ex, "Failed to publish message {MessageId} of type {MessageType}", msg.Id, msg.Type);
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
}