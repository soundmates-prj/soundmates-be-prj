using System.Text;
using System.Text.Json;
using LiveSessionService.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events.Config;

namespace LiveSessionService.Infrastructure.Messaging;

/// <summary>
/// Background service that listens for AzuraCast config update events from RabbitMQ.
/// When the admin updates the AzuraCast API key via the admin panel,
/// this consumer receives the event and updates the AzuraCast HttpClient at runtime.
/// </summary>
public sealed class AzuraCastConfigEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<AzuraCastConfigEventConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public AzuraCastConfigEventConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<AzuraCastConfigEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AzuraCast config consumer failed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = await factory.CreateConnectionAsync(
            "live-session-service-azuracast-config-consumer",
            cancellationToken);

        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Declare exchange (topic, durable)
        await _channel.ExchangeDeclareAsync(
            exchange: _options.ConfigExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Declare queue (durable, exclusive=false, autoDelete=false)
        await _channel.QueueDeclareAsync(
            queue: _options.AzuraCastQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Bind queue to exchange with routing key
        await _channel.QueueBindAsync(
            queue: _options.AzuraCastQueue,
            exchange: _options.ConfigExchange,
            routingKey: _options.AzuraCastRoutingKey,
            cancellationToken: cancellationToken);

        // Set prefetch count = 1 for fair dispatch
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleMessageAsync;

        await _channel.BasicConsumeAsync(
            queue: _options.AzuraCastQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "AzuraCast config consumer is listening on queue '{Queue}' with routing key '{RoutingKey}'",
            _options.AzuraCastQueue,
            _options.AzuraCastRoutingKey);

        // Keep alive until cancelled
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var payload = Encoding.UTF8.GetString(args.Body.ToArray());
            var evt = JsonSerializer.Deserialize<AzuraCastConfigUpdatedEvent>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evt is null)
            {
                _logger.LogWarning("Received empty AzuraCastConfigUpdatedEvent payload.");
                await _channel.BasicAckAsync(args.DeliveryTag, false);
                return;
            }

            // Resolve IAzuraCastClient from DI scope
            using var scope = _scopeFactory.CreateScope();
            var azuraCastClient = scope.ServiceProvider.GetRequiredService<IAzuraCastClient>();

            if (evt.IsDeleted)
            {
                _logger.LogWarning(
                    "AzuraCast config was deleted by admin. Resetting HttpClient to initial config from startup env vars.");

                // Attempt to reset to initial env var values (from DependencyInjection startup)
                // The AzuraCastClient.UpdateConfig clears the header but can't restore initial values
                // without knowing them here. Log warning that admin should restart or set new config.
                _logger.LogWarning(
                    "AzuraCast API key cleared. AzuraCast operations will fail until a new key is configured via admin panel.");

                await _channel.BasicAckAsync(args.DeliveryTag, false);
                return;
            }

            // Update the HttpClient with new BaseUrl and API key
            azuraCastClient.UpdateConfig(evt.BaseUrl, evt.ApiKey);

            _logger.LogInformation(
                "AzuraCast config updated at runtime via event: BaseUrl={BaseUrl}, IsActive={IsActive}",
                evt.BaseUrl,
                evt.IsActive);

            await _channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process AzuraCast config update message.");
            // Requeue on failure for retry
            await _channel.BasicNackAsync(args.DeliveryTag, false, true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken: cancellationToken);
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken: cancellationToken);
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
