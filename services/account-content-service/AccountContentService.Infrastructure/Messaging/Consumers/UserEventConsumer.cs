using System.Text;
using System.Text.Json;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events.Auth;

namespace AccountContentService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes user events from the <c>auth.users</c> exchange and updates the
/// local <c>UserProfileReadModel</c> projection table.
/// </summary>
public sealed class UserEventConsumer : IAsyncDisposable
{
    private const string ExchangeName = "auth.users";
    private const string QueueName = "account-content.user-events";

    private readonly RabbitMqOptions _options;
    private readonly IUserProfileReadModelRepository _repo;
    private readonly ILogger<UserEventConsumer> _logger;
    private readonly ConnectionFactory _factory;

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _started;

    public UserEventConsumer(
        IOptions<RabbitMqOptions> options,
        IUserProfileReadModelRepository repo,
        ILogger<UserEventConsumer> logger)
    {
        _options = options.Value;
        _repo = repo;
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started) return;

        _connection = await _factory.CreateConnectionAsync(
            clientProvidedName: "account-content-service-consumer",
            cancellationToken);

        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Declare exchange
        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Declare queue
        var queueArgs = new Dictionary<string, object?>();
        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken);

        // Bind queue to exchange with wildcard routing key (all user events)
        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: "auth.user.#",
            cancellationToken: cancellationToken);

        // Set prefetch count (process 10 messages at a time)
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var routingKey = ea.RoutingKey;
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            try
            {
                await ProcessMessageAsync(routingKey, json, cancellationToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message with routing key {RoutingKey}: {Json}", routingKey, json);
                // Negative ack — requeue once then move to DLQ
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _started = true;
        _logger.LogInformation(
            "UserEventConsumer started. Listening on queue {Queue} bound to exchange {Exchange}",
            QueueName, ExchangeName);
    }

    private async Task ProcessMessageAsync(string routingKey, string json, CancellationToken ct)
    {
        switch (routingKey)
        {
            case "auth.user.created":
                await HandleUserCreatedAsync(json, ct);
                break;

            case "auth.user.profile.updated":
                await HandleProfileUpdatedAsync(json, ct);
                break;

            case "auth.user.deleted":
                await HandleUserDeletedAsync(json, ct);
                break;

            default:
                _logger.LogDebug("Ignoring event with routing key {RoutingKey}", routingKey);
                break;
        }
    }

    private async Task HandleUserCreatedAsync(string json, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<UserCreatedEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt == null)
        {
            _logger.LogWarning("Failed to deserialize UserCreatedEvent: {Json}", json);
            return;
        }

        var profile = new UserProfileReadModel
        {
            Id = evt.Id,
            FullName = $"{evt.FirstName} {evt.LastName}".Trim(),
            FirstName = evt.FirstName,
            LastName = evt.LastName,
            AvatarUrl = null,
            Email = evt.Email,
            IsPending = false,
            UpdatedAt = evt.CreatedAt,
            CreatedAt = evt.CreatedAt
        };

        await _repo.UpsertAsync(profile, ct);
        _logger.LogInformation("UserProfileReadModel upserted for user {UserId} (created)", evt.Id);
    }

    private async Task HandleProfileUpdatedAsync(string json, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<UserProfileUpdatedEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt == null)
        {
            _logger.LogWarning("Failed to deserialize UserProfileUpdatedEvent: {Json}", json);
            return;
        }

        var profile = new UserProfileReadModel
        {
            Id = evt.Id,
            FullName = $"{evt.FirstName} {evt.LastName}".Trim(),
            FirstName = evt.FirstName,
            LastName = evt.LastName,
            AvatarUrl = evt.Profile?.ProfileImageUrl,
            Email = evt.Email,
            IsPending = false,
            UpdatedAt = evt.UpdatedAt,
            CreatedAt = DateTime.UtcNow // preserved for existing records via upsert
        };

        await _repo.UpsertAsync(profile, ct);
        _logger.LogInformation("UserProfileReadModel upserted for user {UserId} (profile updated)", evt.Id);
    }

    private async Task HandleUserDeletedAsync(string json, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<UserDeletedEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt == null)
        {
            _logger.LogWarning("Failed to deserialize UserDeletedEvent: {Json}", json);
            return;
        }

        await _repo.DeleteAsync(evt.Id, ct);
        _logger.LogInformation("UserProfileReadModel deleted for user {UserId}", evt.Id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.CloseAsync();
        _channel?.Dispose();
        if (_connection != null) await _connection.CloseAsync();
        _connection?.Dispose();
    }
}
