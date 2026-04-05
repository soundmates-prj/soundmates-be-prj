using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Messaging.Consumers.Notifications
{
    public sealed class NotificationEventConsumerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationEventConsumerHostedService> _logger;

        public NotificationEventConsumerHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationEventConsumerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var consumer = scope.ServiceProvider
                    .GetRequiredService<NotificationEventConsumer>();

                await consumer.StartAsync(stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Notification consumer shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification consumer failed");
            }
        }
    }
}
