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
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var retryDelay = TimeSpan.FromSeconds(5);
        const int maxRetryDelaySeconds = 60;

        while (!stoppingToken.IsCancellationRequested)
        {
            // IMPORTANT: scope must stay alive for the entire consumer lifetime
            // because UserEventConsumer holds IUserProfileReadModelRepository (Scoped).
            // Do NOT dispose the scope until the consumer is done.
            var scope = _scopeFactory.CreateAsyncScope();
            try
            {
                var consumer = scope.ServiceProvider.GetRequiredService<UserEventConsumer>();
                await consumer.StartAsync(stoppingToken);

                _logger.LogInformation("UserEventConsumerHostedService: consumer started successfully.");

                // Keep alive until cancellation — scope is still open here
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("UserEventConsumerHostedService is shutting down.");
                await scope.DisposeAsync();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "UserEventConsumerHostedService failed (will retry in {Delay}s). " +
                    "User profile events are NOT being consumed.", retryDelay.TotalSeconds);
            }
            finally
            {
                await scope.DisposeAsync();
            }

            // Exponential backoff before retry
            await Task.Delay(retryDelay, stoppingToken);
            retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, maxRetryDelaySeconds));
        }
    }
}
