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
using AuthQueryService.Domain.Entities;
using MongoDB.Driver;

namespace AuthQueryService.Infrastructure.Messaging
{
    /// <summary>
    /// Clean Role Projection Service - Simplified and maintainable
    /// </summary>
    public sealed class RabbitMqRoleProjectionService : BackgroundService
    {
        private readonly ILogger<RabbitMqRoleProjectionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConnectionFactory _factory;
        private IConnection? _conn;
        private IChannel? _channel;

        private const string Exchange = "auth.users";
        private const string QueueName = "auth-service-query.roles";
        private const int MaxRetryAttempts = 30;
        private const int RetryDelaySeconds = 10;

        public RabbitMqRoleProjectionService(
            IConfiguration cfg,
            ILogger<RabbitMqRoleProjectionService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _factory = CreateConnectionFactory(cfg);

            _logger.LogInformation("RabbitMQ Role Service Config: Host={Host}, Port={Port}", 
                _factory.HostName, _factory.Port);
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
            _logger.LogInformation("?? Starting Role Projection Service...");
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
                
                _logger.LogInformation("? Role Service started successfully!");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("?? Role Service stopping...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Fatal error in Role Service");
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
                    _conn = await _factory.CreateConnectionAsync("auth-query-roles", cancellationToken: ct);
                    _channel = await _conn.CreateChannelAsync(cancellationToken: ct);
                    _logger.LogInformation("? Role Service connected on attempt {Attempt}", attempt);
                    return;
                }
                catch (Exception ex) when (ex is BrokerUnreachableException or SocketException or TimeoutException)
                {
                    if (attempt % 5 == 0)
                        _logger.LogWarning("?? Role Service attempt {Attempt}/{Max}: {Message}", 
                            attempt, MaxRetryAttempts, ex.Message);
                }

                if (attempt < MaxRetryAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), ct);
            }

            _logger.LogError("? Role Service failed after {Attempts} attempts", MaxRetryAttempts);
        }

        private async Task SetupQueueAsync(CancellationToken ct)
        {
            if (_channel == null) throw new InvalidOperationException("Channel not initialized");

            await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

            var routingKeys = new[] { "auth.role.created", "auth.role.updated", "auth.role.deleted" };
            foreach (var rk in routingKeys)
            {
                await _channel.QueueBindAsync(QueueName, Exchange, rk, cancellationToken: ct);
                _logger.LogDebug("?? Role bound: {RoutingKey}", rk);
            }

            _logger.LogInformation("?? Role queue setup complete");
        }

        private async Task StartConsumingAsync(CancellationToken ct)
        {
            if (_channel == null) throw new InvalidOperationException("Channel not initialized");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await ProcessEventAsync(ea, ct);

            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: ct);
            _logger.LogInformation("?? Role Service listening on: {Queue}", QueueName);
        }

        private async Task ProcessEventAsync(BasicDeliverEventArgs ea, CancellationToken ct)
        {
            var eventType = ea.RoutingKey;
            
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogDebug("?? Role event: {EventType}", eventType);

                await using var scope = _serviceProvider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                var collection = db.GetCollection<UserRole>("roles_read");

                await HandleRoleEventAsync(eventType, json, collection, ct);

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
                _logger.LogDebug("? Role processed: {EventType}", eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Role event failed: {EventType}", eventType);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
            }
        }

        private async Task HandleRoleEventAsync(
            string eventType, 
            string json, 
            IMongoCollection<UserRole> collection, 
            CancellationToken ct)
        {
            using var doc = JsonDocument.Parse(UnwrapPayload(json));
            var root = doc.RootElement;

            switch (eventType)
            {
                case "auth.role.created":
                case "auth.role.updated":
                    var id = GetGuid(root, "id", "Id");
                    var name = GetString(root, "name", "Name");

                    var role = new UserRole { Id = id, Name = name };
                    await collection.ReplaceOneAsync(
                        r => r.Id == id, 
                        role, 
                        new ReplaceOptions { IsUpsert = true }, 
                        cancellationToken: ct);

                    _logger.LogInformation("? Role {Action}: {Name}", 
                        eventType.Contains("created") ? "created" : "updated", name);
                    break;

                case "auth.role.deleted":
                    var roleId = GetGuid(root, "id", "Id");
                    await collection.DeleteOneAsync(r => r.Id == roleId, cancellationToken: ct);
                    _logger.LogInformation("? Role deleted: {Id}", roleId);
                    break;

                default:
                    _logger.LogWarning("?? Unknown role event: {EventType}", eventType);
                    break;
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

        private Guid GetGuid(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (element.TryGetProperty(name, out var prop) && 
                    Guid.TryParse(prop.GetString(), out var guid))
                    return guid;
            }
            throw new JsonException($"Guid not found. Tried: {string.Join(", ", names)}");
        }

        private string GetString(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (element.TryGetProperty(name, out var prop) && 
                    prop.ValueKind == JsonValueKind.String)
                    return prop.GetString() ?? string.Empty;
            }
            throw new JsonException($"String not found. Tried: {string.Join(", ", names)}");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("?? Stopping Role Service...");
            
            if (_channel != null) await _channel.CloseAsync(cancellationToken);
            if (_conn != null) await _conn.CloseAsync(cancellationToken);
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("? Role Service stopped");
        }
    }
}
