using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Hosted service that starts the RabbitMQ user-event consumer when the
/// application starts and keeps it running until shutdown.
/// </summary>
public sealed class UserEventConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserEventConsumerHostedService> _logger;

    public UserEventConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<UserEventConsumerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the app a moment to fully start before connecting to RabbitMQ
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var consumer = scope.ServiceProvider.GetRequiredService<UserEventConsumer>();
            await consumer.StartAsync(stoppingToken);

            // Keep alive until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("UserEventConsumerHostedService is shutting down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserEventConsumerHostedService failed to start. " +
                "User profile events will not be consumed until the service restarts.");
        }
    }
}
