using System;
using System.Text;
using System.Text.Json;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using AuthQueryService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace AuthQueryService.Infrastructure.Messaging
{
    public sealed class RabbitMqRoleProjectionService : BackgroundService
    {
        private readonly ILogger<RabbitMqRoleProjectionService> _logger;
        private readonly IServiceProvider _sp;
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
            IServiceProvider sp)
        {
            _logger = logger;
            _sp = sp;

            // Read from environment variables first (Docker/Kubernetes), then from config
            // Environment variables take precedence over appsettings.json
            var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") 
                        ?? cfg["RabbitMq:HostName"]?.Replace("${RABBITMQ_HOST}", "")?.Trim()
                        ?? cfg["RABBITMQ_HOST"] 
                        ?? "rabbitmq";
            
            // Remove placeholder syntax if present
            if (hostName.Contains("${"))
            {
                hostName = "rabbitmq"; // Default to service name in Docker
            }
            
            var portStr = Environment.GetEnvironmentVariable("RABBITMQ_PORT") 
                       ?? cfg["RabbitMq:Port"]?.Replace("${RABBITMQ_PORT}", "")?.Trim()
                       ?? cfg["RABBITMQ_PORT"] 
                       ?? "5672";
            
            var userName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") 
                        ?? cfg["RabbitMq:UserName"]?.Replace("${RABBITMQ_USERNAME}", "")?.Trim()
                        ?? cfg["RABBITMQ_USERNAME"] 
                        ?? "guest";
            
            var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") 
                        ?? cfg["RabbitMq:Password"]?.Replace("${RABBITMQ_PASSWORD}", "")?.Trim()
                        ?? cfg["RABBITMQ_PASSWORD"] 
                        ?? "guest";
            
            var virtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VIRTUALHOST") 
                           ?? cfg["RabbitMq:VirtualHost"]?.Replace("${RABBITMQ_VIRTUALHOST}", "")?.Trim()
                           ?? cfg["RABBITMQ_VIRTUALHOST"] 
                           ?? "/";
            
            _factory = new ConnectionFactory
            {
                HostName = hostName,
                Port = int.TryParse(portStr, out var p) ? p : 5672,
                UserName = userName,
                Password = password,
                VirtualHost = virtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(30)
            };

            _logger.LogInformation("RabbitMQ Role Projection Configuration: Host={Host}, Port={Port}, VHost={VHost}, User={User}",
                _factory.HostName, _factory.Port, _factory.VirtualHost, _factory.UserName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting RabbitMQ Role Projection Service...");
            
            // Initial delay to let RabbitMQ start
            _logger.LogInformation("Waiting 15 seconds for RabbitMQ to be ready...");
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            try
            {
                await ConnectWithRetryAsync(stoppingToken);

                if (_conn == null || _channel == null)
                {
                    _logger.LogError("Failed to establish RabbitMQ connection. Service will not process events.");
                    return;
                }

                await SetupExchangeAndQueueAsync(stoppingToken);
                await StartConsumingAsync(stoppingToken);
                
                _logger.LogInformation("RabbitMQ Role Projection Service started successfully!");
                
                // Keep the service running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RabbitMQ Role Projection Service is stopping (cancellation requested)...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in RabbitMQ Role Projection Service");
                throw;
            }
        }

        private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connecting to RabbitMQ: {Host}:{Port}", _factory.HostName, _factory.Port);

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Connection attempts cancelled");
                    return;
                }

                try
                {
                    if (attempt % 5 == 1 || attempt <= 3)
                    {
                        _logger.LogInformation("Connection attempt {Attempt}/{Max}...", attempt, MaxRetryAttempts);
                    }
                    
                    _conn = await _factory.CreateConnectionAsync(
                        clientProvidedName: "auth-query-service-roles", 
                        cancellationToken: stoppingToken);
                    
                    _channel = await _conn.CreateChannelAsync(cancellationToken: stoppingToken);
                    
                    _logger.LogInformation("Connected to RabbitMQ successfully on attempt {Attempt}!", attempt);
                    return;
                }
                catch (BrokerUnreachableException ex)
                {
                    if (attempt % 5 == 0 || attempt <= 3)
                    {
                        _logger.LogWarning("RabbitMQ broker unreachable (attempt {Attempt}/{Max}): {Message}", 
                            attempt, MaxRetryAttempts, ex.Message);
                    }
                }
                catch (SocketException ex)
                {
                    if (attempt % 5 == 0 || attempt <= 3)
                    {
                        _logger.LogWarning("Socket error connecting to RabbitMQ (attempt {Attempt}/{Max}): {Message}", 
                            attempt, MaxRetryAttempts, ex.Message);
                    }
                }
                catch (TimeoutException ex)
                {
                    if (attempt % 5 == 0 || attempt <= 3)
                    {
                        _logger.LogWarning("Connection timeout (attempt {Attempt}/{Max}): {Message}", 
                            attempt, MaxRetryAttempts, ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected error connecting to RabbitMQ (attempt {Attempt}/{Max})", 
                        attempt, MaxRetryAttempts);
                }

                if (attempt < MaxRetryAttempts)
                {
                    if (attempt % 5 == 0 || attempt <= 3)
                    {
                        _logger.LogInformation("Retrying in {Seconds} seconds...", RetryDelaySeconds);
                    }
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
                }
            }

            _logger.LogError("Could not connect to RabbitMQ after {Attempts} attempts. Giving up.", MaxRetryAttempts);
        }

        private async Task SetupExchangeAndQueueAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("Channel is not initialized");
            }

            _logger.LogInformation("Declaring exchange: {Exchange}", Exchange);
            await _channel.ExchangeDeclareAsync(
                exchange: Exchange, 
                type: ExchangeType.Topic, 
                durable: true, 
                autoDelete: false, 
                arguments: null, 
                cancellationToken: stoppingToken);

            _logger.LogInformation("Declaring queue: {Queue}", QueueName);
            await _channel.QueueDeclareAsync(
                queue: QueueName, 
                durable: true, 
                exclusive: false, 
                autoDelete: false, 
                arguments: null, 
                cancellationToken: stoppingToken);

            var routingKeys = new[] { "auth.role.created", "auth.role.updated", "auth.role.deleted" };
            foreach (var rk in routingKeys)
            {
                await _channel.QueueBindAsync(
                    queue: QueueName, 
                    exchange: Exchange, 
                    routingKey: rk, 
                    arguments: null, 
                    cancellationToken: stoppingToken);
                
                _logger.LogInformation("Bound queue to routing key: {RoutingKey}", rk);
            }
        }

        private async Task StartConsumingAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("Channel is not initialized");
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            consumer.ReceivedAsync += async (_, ea) =>
            {
                    try
                    {
                        var routingKey = ea.RoutingKey;
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        
                        _logger.LogDebug("Received role event: {RoutingKey}", routingKey);
                        
                        await ProjectAsync(routingKey, json, stoppingToken);
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        
                        _logger.LogDebug("Role event processed successfully: {RoutingKey}", routingKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Role projection failed for event. Nacking message.");
                    
                    if (_channel != null)
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                    }
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName, 
                autoAck: false, 
                consumer: consumer, 
                cancellationToken: stoppingToken);
            
            _logger.LogInformation("Now listening for role events on queue: {Queue}", QueueName);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping RabbitMQ Role Projection Service...");
            
            try 
            { 
                if (_channel is not null) 
                {
                    await _channel.CloseAsync(cancellationToken);
                    _logger.LogInformation("Channel closed");
                }
            } 
            catch (Exception ex) 
            {
                _logger.LogWarning(ex, "Error closing channel");
            }
            
            try 
            { 
                if (_conn is not null) 
                {
                    await _conn.CloseAsync(cancellationToken);
                    _logger.LogInformation("Connection closed");
                }
            } 
            catch (Exception ex) 
            {
                _logger.LogWarning(ex, "Error closing connection");
            }
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("RabbitMQ Role Projection Service stopped");
        }

        private async Task ProjectAsync(string type, string payload, CancellationToken ct)
        {
            _logger.LogDebug("Projecting role event type: {Type}", type);
            
            // Handle double-serialized JSON efficiently - parse once
            string actualPayload = payload;
            try
            {
                using var testDoc = JsonDocument.Parse(payload);
                if (testDoc.RootElement.ValueKind == JsonValueKind.String)
                {
                    actualPayload = testDoc.RootElement.GetString() ?? payload;
                    _logger.LogDebug("Unwrapped double-serialized JSON");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON payload for event type: {Type}", type);
                throw;
            }
            
            await using var scope = _sp.CreateAsyncScope();
            var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
            var rolesCollection = mongoDatabase.GetCollection<UserRole>("roles_read");

            switch (type)
            {
                case "auth.role.created":
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(actualPayload);
                        var root = doc.RootElement;
                        
                        var id = GetGuidProperty(root, "id", "Id", "roleId", "RoleId");
                        var name = GetStringProperty(root, "name", "Name");
                        
                        if (id == Guid.Empty)
                        {
                            _logger.LogError("Failed to extract role ID from payload. Payload: {Payload}", actualPayload);
                            // Try to get ID from the role object if nested
                            if (root.TryGetProperty("role", out var roleElement))
                            {
                                id = GetGuidProperty(roleElement, "id", "Id");
                            }
                        }
                        
                        if (string.IsNullOrEmpty(name))
                        {
                            _logger.LogError("Invalid role data - Name is empty. Skipping.");
                            break;
                        }
                        
                        // If ID is empty, we'll still create the role with name
                        // The role will be identified by name, and we'll generate a new ID for MongoDB
                        if (id == Guid.Empty)
                        {
                            _logger.LogWarning("Role ID is empty for role {Name}. Generating new ID for MongoDB.", name);
                            id = Guid.NewGuid();
                        }
                        
                        _logger.LogDebug("Creating role in MongoDB: {Name} ({Id})", name, id);
                        
                        var role = new UserRole
                        {
                            Id = id,
                            Name = name
                        };
                        
                        // Use name as unique identifier instead of ID (in case ID is wrong)
                        var filter = Builders<UserRole>.Filter.Eq(r => r.Name, name);
                        await rolesCollection.ReplaceOneAsync(filter, role, new ReplaceOptions { IsUpsert = true }, cancellationToken: ct);
                        
                        _logger.LogDebug("Role created/updated in MongoDB: {Name} ({Id})", name, id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse RoleCreatedEvent. Payload: {Payload}", actualPayload);
                        throw;
                    }
                    break;
                }
                case "auth.role.updated":
                {
                    try
                    {
                        _logger.LogInformation("Raw payload: {Payload}", actualPayload);
                        using var doc = JsonDocument.Parse(actualPayload);
                        var root = doc.RootElement;
                        
                        var id = GetGuidProperty(root, "id", "Id", "roleId", "RoleId");
                        var name = GetStringProperty(root, "name", "Name");
                        
                        if (id == Guid.Empty)
                        {
                            _logger.LogError("Failed to extract role ID from payload. Payload: {Payload}", actualPayload);
                            if (root.TryGetProperty("role", out var roleElement))
                            {
                                id = GetGuidProperty(roleElement, "id", "Id");
                            }
                        }
                        
                        if (id == Guid.Empty || string.IsNullOrEmpty(name))
                        {
                            _logger.LogError("Invalid role data - ID: {Id}, Name: {Name}. Skipping.", id, name);
                            break;
                        }
                        
                        _logger.LogDebug("Updating role in MongoDB: {Name} ({Id})", name, id);
                        
                        var role = new UserRole
                        {
                            Id = id,
                            Name = name
                        };
                        
                        // Use name as unique identifier to ensure we update the correct role
                        var filter = Builders<UserRole>.Filter.Eq(r => r.Name, name);
                        await rolesCollection.ReplaceOneAsync(filter, role, new ReplaceOptions { IsUpsert = true }, cancellationToken: ct);
                        
                        _logger.LogDebug("Role updated in MongoDB: {Name} ({Id})", name, id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse RoleUpdatedEvent. Payload: {Payload}", actualPayload);
                        throw;
                    }
                    break;
                }
                case "auth.role.deleted":
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(actualPayload);
                        var root = doc.RootElement;
                        
                        var id = GetGuidProperty(root, "id", "Id", "roleId", "RoleId");
                        
                        _logger.LogDebug("Deleting role from MongoDB: {Id}", id);
                        
                        var filter = Builders<UserRole>.Filter.Eq(r => r.Id, id);
                        await rolesCollection.DeleteOneAsync(filter, cancellationToken: ct);
                        
                        _logger.LogDebug("Role deleted from MongoDB: {Id}", id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse RoleDeletedEvent");
                        throw;
                    }
                    break;
                }
                default:
                    _logger.LogWarning("Unknown role event type: {Type}", type);
                    break;
            }
        }

        private Guid GetGuidProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        var str = prop.GetString();
                        if (!string.IsNullOrEmpty(str) && Guid.TryParse(str, out var guid))
                            return guid;
                    }
                    else if (prop.ValueKind == JsonValueKind.Object)
                    {
                        // Try to get value from nested object
                        if (prop.TryGetProperty("value", out var valueProp) && valueProp.ValueKind == JsonValueKind.String)
                        {
                            var str = valueProp.GetString();
                            if (!string.IsNullOrEmpty(str) && Guid.TryParse(str, out var guid2))
                                return guid2;
                        }
                    }
                    else if (prop.ValueKind == JsonValueKind.Array && prop.GetArrayLength() > 0)
                    {
                        // Sometimes GUIDs are serialized as arrays
                        var first = prop[0];
                        if (first.ValueKind == JsonValueKind.String)
                        {
                            var str = first.GetString();
                            if (!string.IsNullOrEmpty(str) && Guid.TryParse(str, out var guid3))
                                return guid3;
                        }
                    }
                }
            }
            // If not found, log warning and return empty GUID
            _logger.LogWarning("Required Guid property not found. Tried: {PropertyNames}. Returning empty GUID.", string.Join(", ", propertyNames));
            return Guid.Empty;
        }

        private static string GetStringProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                        return prop.GetString() ?? string.Empty;
                }
            }
            return string.Empty;
        }
    }
}

