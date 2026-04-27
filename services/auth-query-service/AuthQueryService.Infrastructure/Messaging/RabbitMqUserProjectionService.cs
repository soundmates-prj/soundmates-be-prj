using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using AuthQueryService.Infrastructure.Messaging.EventHandlers;

namespace AuthQueryService.Infrastructure.Messaging
{
    /// <summary>
    /// Clean User Projection Service - Refactored from 700 lines to ~200 lines
    /// Uses Strategy Pattern with event handlers for better maintainability
    /// </summary>
    public sealed class RabbitMqUserProjectionService : BackgroundService
    {
        private readonly ILogger<RabbitMqUserProjectionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConnectionFactory _factory;
        private readonly Dictionary<string, Type> _handlerTypes;
        private IConnection? _conn;
        private IChannel? _channel;

        private const string Exchange = "auth.users";
        private const string QueueName = "auth-service-query.users";
        private const int MaxRetryAttempts = 30;
        private const int RetryDelaySeconds = 10;

        public RabbitMqUserProjectionService(
            IConfiguration cfg,
            ILogger<RabbitMqUserProjectionService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _factory = CreateConnectionFactory(cfg);

            // Build handler map at startup
            _handlerTypes = new Dictionary<string, Type>();
            using var scope = serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IUserEventHandler>();
            foreach (var handler in handlers)
            {
                _handlerTypes[handler.EventType] = handler.GetType();
            }

            _logger.LogInformation("RabbitMQ Config: Host={Host}, Port={Port}, Handlers={Count}", 
                _factory.HostName, _factory.Port, _handlerTypes.Count);
        }

        private static ConnectionFactory CreateConnectionFactory(IConfiguration cfg)
        {
            var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") 
                        ?? cfg["RabbitMq:HostName"]?.Replace("${RABBITMQ_HOST}", "").Trim() 
                        ?? "rabbitmq";
            
            var port = int.TryParse(
                Environment.GetEnvironmentVariable("RABBITMQ_PORT") 
                ?? cfg["RabbitMq:Port"]?.Replace("${RABBITMQ_PORT}", "").Trim(), 
                out var p) ? p : 5672;

            return new ConnectionFactory
            {
                HostName = hostName.Contains("${") ? "rabbitmq" : hostName,
                Port = port,
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? cfg["RabbitMq:UserName"] ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? cfg["RabbitMq:Password"] ?? "guest",
                VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VIRTUALHOST") ?? cfg["RabbitMq:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(30)
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("?? Starting User Projection Service...");
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            try
            {
                await ConnectWithRetryAsync(stoppingToken);
                if (_conn == null || _channel == null)
                {
                    _logger.LogError("? Failed to connect. Service stopped.");
                    return;
                }

                await SetupQueueAsync(stoppingToken);
                await StartConsumingAsync(stoppingToken);
                
                _logger.LogInformation("? Service started successfully!");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("?? Service stopping...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Fatal error");
                throw;
            }
        }

        private async Task ConnectWithRetryAsync(CancellationToken ct)
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                if (ct.IsCancellationRequested) return;

                try
                {
                    _conn = await _factory.CreateConnectionAsync("auth-query-users", cancellationToken: ct);
                    _channel = await _conn.CreateChannelAsync(cancellationToken: ct);
                    _logger.LogInformation("? Connected on attempt {Attempt}", attempt);
                    return;
                }
                catch (Exception ex) when (ex is BrokerUnreachableException or SocketException or TimeoutException)
                {
                    if (attempt % 5 == 0)
                        _logger.LogWarning("?? Attempt {Attempt}/{Max}: {Message}", attempt, MaxRetryAttempts, ex.Message);
                }

                if (attempt < MaxRetryAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), ct);
            }

            _logger.LogError("? Failed after {Attempts} attempts", MaxRetryAttempts);
        }

        private async Task SetupQueueAsync(CancellationToken ct)
        {
            if (_channel == null) throw new InvalidOperationException("Channel not initialized");

            await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

            foreach (var eventType in _handlerTypes.Keys)
            {
                await _channel.QueueBindAsync(QueueName, Exchange, eventType, cancellationToken: ct);
                _logger.LogDebug("?? Bound: {EventType}", eventType);
            }

            _logger.LogInformation("?? Queue setup complete. {Count} event types", _handlerTypes.Count);
        }

        private async Task StartConsumingAsync(CancellationToken ct)
        {
            if (_channel == null) throw new InvalidOperationException("Channel not initialized");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await ProcessEventAsync(ea, ct);

            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: ct);
            _logger.LogInformation("?? Listening on: {Queue}", QueueName);
        }

        private async Task ProcessEventAsync(BasicDeliverEventArgs ea, CancellationToken ct)
        {
            var eventType = ea.RoutingKey;
            
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogDebug("?? Event: {EventType}", eventType);

                using var doc = JsonDocument.Parse(UnwrapPayload(json));
                
                if (!_handlerTypes.ContainsKey(eventType))
                {
                    _logger.LogWarning("?? No handler: {EventType}", eventType);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
                    return;
                }

                await using var scope = _serviceProvider.CreateAsyncScope();
                
                // Get all handlers and find the one matching this event type
                var handlers = scope.ServiceProvider.GetServices<IUserEventHandler>();
                var handler = handlers.FirstOrDefault(h => h.EventType == eventType);
                
                if (handler == null)
                {
                    _logger.LogWarning("?? Handler not found in DI: {EventType}", eventType);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
                    return;
                }

                await handler.HandleAsync(doc, ct);

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
                _logger.LogDebug("? Processed: {EventType}", eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed: {EventType}", eventType);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
            }
        }

        private string UnwrapPayload(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.String)
                    return doc.RootElement.GetString() ?? json;
            }
            catch { }
            return json;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("?? Stopping...");
            
            if (_channel != null) await _channel.CloseAsync(cancellationToken);
            if (_conn != null) await _conn.CloseAsync(cancellationToken);
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("? Stopped");
        }
    }
}
