using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using AuthQueryService.Infrastructure.DAO.Interfaces;
using AuthQueryService.Domain.Entities.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;

namespace AuthQueryService.Infrastructure.Messaging
{
    public sealed class RabbitMqUserProjectionService : BackgroundService
    {
        private readonly ILogger<RabbitMqUserProjectionService> _logger;
        private readonly IServiceProvider _sp;
        private readonly ConnectionFactory _factory;
        private IConnection? _conn;
        private IChannel? _channel;

        private const string Exchange = "auth.users";
        private const string QueueName = "auth-service-query.users";
        private const int MaxRetryAttempts = 30;
        private const int RetryDelaySeconds = 10;

        public RabbitMqUserProjectionService(
            IConfiguration cfg,
            ILogger<RabbitMqUserProjectionService> logger,
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

            _logger.LogInformation("RabbitMQ Configuration: Host={Host}, Port={Port}, VHost={VHost}, User={User}", 
                _factory.HostName, _factory.Port, _factory.VirtualHost, _factory.UserName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting RabbitMQ User Projection Service...");
            
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
                
                _logger.LogInformation("RabbitMQ User Projection Service started successfully!");
                
                // Keep the service running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RabbitMQ User Projection Service is stopping (cancellation requested)...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in RabbitMQ User Projection Service");
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
                        clientProvidedName: "auth-query-service-users", 
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

            _logger.LogInformation("📢 Declaring exchange: {Exchange}", Exchange);
            await _channel.ExchangeDeclareAsync(
                exchange: Exchange, 
                type: ExchangeType.Topic, 
                durable: true, 
                autoDelete: false, 
                arguments: null, 
                cancellationToken: stoppingToken);

            _logger.LogInformation("📬 Declaring queue: {Queue}", QueueName);
            await _channel.QueueDeclareAsync(
                queue: QueueName, 
                durable: true, 
                exclusive: false, 
                autoDelete: false, 
                arguments: null, 
                cancellationToken: stoppingToken);

            var routingKeys = new[] 
            { 
                "auth.user.registration.successful", 
                "auth.user.created", 
                "auth.user.updated", 
                "auth.user.deleted",
                "auth.user.profile.updated",
                "auth.user.password.changed",
                "auth.user.password.reset"
            };

            foreach (var rk in routingKeys)
            {
                await _channel.QueueBindAsync(
                    queue: QueueName, 
                    exchange: Exchange, 
                    routingKey: rk, 
                    arguments: null, 
                    cancellationToken: stoppingToken);
                
                _logger.LogInformation("🔗 Bound queue to routing key: {RoutingKey}", rk);
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
                    
                    _logger.LogDebug("Received event: {RoutingKey}", routingKey);
                    
                    await ProjectAsync(routingKey, json, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    
                    _logger.LogDebug("Event processed successfully: {RoutingKey}", routingKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Projection failed for event. Nacking message.");
                    
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

            _logger.LogInformation("Now listening for user events on queue: {Queue}", QueueName);
        }

        private async Task ProjectAsync(string type, string payload, CancellationToken ct)
        {
            _logger.LogDebug("Projecting event type: {Type}", type);
            
            // Handle double-serialized JSON efficiently - parse once and keep document alive
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
            var dao = scope.ServiceProvider.GetRequiredService<IUserReadDAO>();

            switch (type)
            {
                case "auth.user.registration.successful":
                case "auth.user.created":
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(actualPayload);
                        var root = doc.RootElement;
                        
                        JsonElement userData = root;
                        
                        if (root.TryGetProperty("data", out var dataElement))
                            userData = dataElement;
                        else if (root.TryGetProperty("user", out var userElement))
                            userData = userElement;
                        else if (root.TryGetProperty("Data", out var dataElement2))
                            userData = dataElement2;
                        else if (root.TryGetProperty("User", out var userElement2))
                            userData = userElement2;
                        
                        var id = GetGuidProperty(userData, "id", "Id", "userId", "UserId");
                        var username = GetStringProperty(userData, "username", "Username", "userName", "UserName");
                        var email = GetStringProperty(userData, "email", "Email");
                        // Support both old fullName format (for backward compatibility) and new firstName/lastName format
                        var firstName = GetOptionalStringProperty(userData, "firstName", "FirstName", "first_name");
                        var lastName = GetOptionalStringProperty(userData, "lastName", "LastName", "last_name");
                        
                        // Backward compatibility: if firstName/lastName not found, try to split fullName
                        if (string.IsNullOrEmpty(firstName) && userData.TryGetProperty("fullName", out var fullNameProp) && fullNameProp.ValueKind == JsonValueKind.String)
                        {
                            var fullName = fullNameProp.GetString();
                            if (!string.IsNullOrEmpty(fullName))
                            {
                                var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                                firstName = parts.Length > 0 ? parts[0] : string.Empty;
                                lastName = parts.Length > 1 ? parts[1] : string.Empty;
                            }
                        }
                        
                        var roleId = GetNullableGuidProperty(userData, "roleId", "RoleId", "role_id");
                        var roleName = GetStringProperty(userData, "roleName", "RoleName", "role_name");
                        var isActive = userData.TryGetProperty("isActive", out var isActiveProp) && isActiveProp.ValueKind == JsonValueKind.True
                            ? true
                            : (userData.TryGetProperty("IsActive", out var isActiveProp2) && isActiveProp2.ValueKind == JsonValueKind.True);
                        
                        _logger.LogDebug("Creating user in MongoDB: {Username} ({Id})", username, id);
                        
                        var userModel = new UserReadModel
                        {
                            Id = id,
                            Username = username,
                            Email = email,
                            FirstName = firstName,
                            LastName = lastName,
                            RoleId = roleId,
                            RoleName = roleName,
                            IsActive = isActive,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        
                        await dao.UpsertAsync(userModel);
                        _logger.LogDebug("User created in MongoDB: {Username}", username);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse UserCreatedEvent");
                        throw;
                    }
                    break;
                }
                case "auth.user.updated":
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(actualPayload);
                        var root = doc.RootElement;
                        
                        var userId = GetGuidProperty(root, "id", "Id", "userId", "UserId");
                        var username = GetStringProperty(root, "username", "Username", "userName", "UserName");
                        var email = GetStringProperty(root, "email", "Email");
                        var firstName = GetOptionalStringProperty(root, "firstName", "FirstName", "first_name");
                        var lastName = GetOptionalStringProperty(root, "lastName", "LastName", "last_name");
                        var roleId = GetNullableGuidProperty(root, "roleId", "RoleId", "role_id");
                        var roleName = GetStringProperty(root, "roleName", "RoleName", "role_name");
                        
                        // Backward compatibility: if firstName/lastName not found, try fullName
                        if (string.IsNullOrEmpty(firstName) && root.TryGetProperty("fullName", out var fullNameProp) && fullNameProp.ValueKind == JsonValueKind.String)
                        {
                            var fullName = fullNameProp.GetString();
                            if (!string.IsNullOrEmpty(fullName))
                            {
                                var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                                firstName = parts.Length > 0 ? parts[0] : string.Empty;
                                lastName = parts.Length > 1 ? parts[1] : string.Empty;
                            }
                        }
                        
                        _logger.LogDebug("Updating user in MongoDB: {Id}", userId);
                        
                        var existing = await dao.GetByIdAsync(userId);
                        if (existing is null)
                        {
                            _logger.LogWarning("⚠User not found in MongoDB for update: {Id}", userId);
                            break;
                        }
                        
                        if (!string.IsNullOrEmpty(username)) existing.Username = username;
                        if (!string.IsNullOrEmpty(email)) existing.Email = email;
                        if (!string.IsNullOrEmpty(firstName)) existing.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) existing.LastName = lastName;
                        if (roleId.HasValue) existing.RoleId = roleId;
                        if (!string.IsNullOrEmpty(roleName)) existing.RoleName = roleName;
                        if (root.TryGetProperty("isActive", out var isActiveProp) && isActiveProp.ValueKind == JsonValueKind.True)
                            existing.IsActive = true;
                        else if (root.TryGetProperty("IsActive", out var isActiveProp2) && isActiveProp2.ValueKind == JsonValueKind.True)
                            existing.IsActive = true;
                        else if (root.TryGetProperty("isActive", out var isActiveProp3) && isActiveProp3.ValueKind == JsonValueKind.False)
                            existing.IsActive = false;
                        else if (root.TryGetProperty("IsActive", out var isActiveProp4) && isActiveProp4.ValueKind == JsonValueKind.False)
                            existing.IsActive = false;
                        existing.UpdatedAt = DateTime.UtcNow;
                        
                        await dao.UpsertAsync(existing);
                        _logger.LogDebug("User updated in MongoDB: {Id}", userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse UserUpdatedEvent");
                        throw;
                    }
                    break;
                }
                case "auth.user.profile.updated":
                case "auth.user.password.changed":
                case "auth.user.password.reset":
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(actualPayload);
                        var root = doc.RootElement;
                        
                        var userId = GetGuidProperty(root, "id", "Id", "userId", "UserId");
                        
                        _logger.LogDebug("Updating user in MongoDB for event {Type}: {Id}", type, userId);
                        
                        var existing = await dao.GetByIdAsync(userId);
                        if (existing is null)
                        {
                            _logger.LogWarning("⚠User not found in MongoDB for {Type}: {Id}", type, userId);
                            break;
                        }
                        
                        if (type == "auth.user.profile.updated")
                        {
                            var firstName = GetOptionalStringProperty(root, "firstName", "FirstName", "first_name");
                            var lastName = GetOptionalStringProperty(root, "lastName", "LastName", "last_name");
                            
                            // Backward compatibility
                            if (string.IsNullOrEmpty(firstName) && root.TryGetProperty("fullName", out var fullNameProp) && fullNameProp.ValueKind == JsonValueKind.String)
                            {
                                var fullName = fullNameProp.GetString();
                                if (!string.IsNullOrEmpty(fullName))
                                {
                                    var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                                    firstName = parts.Length > 0 ? parts[0] : string.Empty;
                                    lastName = parts.Length > 1 ? parts[1] : string.Empty;
                                }
                            }
                            
                            if (!string.IsNullOrEmpty(firstName)) existing.FirstName = firstName;
                            if (!string.IsNullOrEmpty(lastName)) existing.LastName = lastName;
                            
                            // Update profile fields if present
                            if (root.TryGetProperty("profile", out var profileProp) && profileProp.ValueKind == JsonValueKind.Object)
                            {
                                var bio = GetOptionalStringProperty(profileProp, "bio", "Bio");
                                var phone = GetOptionalStringProperty(profileProp, "phone", "Phone");
                                var gender = GetOptionalStringProperty(profileProp, "gender", "Gender");
                                var profileImageUrl = GetOptionalStringProperty(profileProp, "profileImageUrl", "ProfileImageUrl", "profile_image_url");
                                var backgroundImageUrl = GetOptionalStringProperty(profileProp, "backgroundImageUrl", "BackgroundImageUrl", "background_image_url");
                                var location = GetOptionalStringProperty(profileProp, "location", "Location");
                                var website = GetOptionalStringProperty(profileProp, "website", "Website");
                                
                                if (bio != null) existing.Bio = bio;
                                if (phone != null) existing.Phone = phone;
                                if (gender != null) existing.Gender = gender;
                                if (profileImageUrl != null) existing.ProfileImageUrl = profileImageUrl;
                                if (backgroundImageUrl != null) existing.BackgroundImageUrl = backgroundImageUrl;
                                if (location != null) existing.Location = location;
                                if (website != null) existing.Website = website;
                                
                                // Handle date of birth
                                if (profileProp.TryGetProperty("dateOfBirth", out var dobProp) || 
                                    profileProp.TryGetProperty("DateOfBirth", out dobProp) ||
                                    profileProp.TryGetProperty("date_of_birth", out dobProp))
                                {
                                    if (dobProp.ValueKind == JsonValueKind.String && DateTime.TryParse(dobProp.GetString(), out var dob))
                                        existing.DateOfBirth = dob;
                                    else if (dobProp.ValueKind == JsonValueKind.Null)
                                        existing.DateOfBirth = null;
                                }
                            }
                        }
                        
                        existing.UpdatedAt = DateTime.UtcNow;
                        
                        await dao.UpsertAsync(existing);
                        _logger.LogDebug("User updated in MongoDB for {Type}: {Id}", type, userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse {Type} event", type);
                        throw;
                    }
                    break;
                }
                case "auth.user.deleted":
                {
                    var e = JsonSerializer.Deserialize<UserDeletedEvent>(payload, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    })!;
                    
                    _logger.LogDebug("Deleting user from MongoDB: {Id}", e.id);
                    await dao.DeleteAsync(e.id);
                    _logger.LogDebug("User deleted from MongoDB: {Id}", e.id);
                    break;
                }
                default:
                    _logger.LogWarning("Unknown event type: {Type}", type);
                    break;
            }
        }

        private static Guid GetGuidProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String && Guid.TryParse(prop.GetString(), out var guid))
                        return guid;
                }
            }
            throw new JsonException($"Required Guid property not found. Tried: {string.Join(", ", propertyNames)}");
        }

        private static Guid? GetNullableGuidProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String && Guid.TryParse(prop.GetString(), out var guid))
                        return guid;
                    if (prop.ValueKind == JsonValueKind.Null)
                        return null;
                }
            }
            return null;
        }

        private static string GetStringProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString() ?? string.Empty;
                }
            }
            throw new JsonException($"Required string property not found. Tried: {string.Join(", ", propertyNames)}");
        }

        private static string? GetOptionalStringProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString();
                }
            }
            return null;
        }

        private sealed record UserCreatedEvent(Guid id, string username, string email, string? fullName, Guid? roleId, DateTimeOffset occurredAt);
        private sealed record UserUpdatedEvent(Guid id, string? username, string? email, string? fullName, Guid? roleId, DateTimeOffset occurredAt);
        private sealed record UserDeletedEvent(Guid id, DateTimeOffset occurredAt);

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping RabbitMQ User Projection Service...");
            
            try 
            { 
                if (_channel is not null) 
                {
                    await _channel.CloseAsync(cancellationToken);
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
                }
            } 
            catch (Exception ex) 
            {
                _logger.LogWarning(ex, "Error closing connection");
            }
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("RabbitMQ User Projection Service stopped");
        }
    }
}