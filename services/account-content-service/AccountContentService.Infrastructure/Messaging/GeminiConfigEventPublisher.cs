using System.Text;
using System.Text.Json;
using AccountContentService.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Contracts.Events;

namespace AccountContentService.Infrastructure.Messaging;

public sealed class GeminiConfigEventPublisher : IGeminiConfigEventPublisher
{
    private readonly ConnectionFactory _factory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<GeminiConfigEventPublisher> _logger;

    private IConnection? _connection;

    public GeminiConfigEventPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<GeminiConfigEventPublisher> logger)
    {
        _options = options.Value;
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

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        _connection = await _factory.CreateConnectionAsync("account-content-service-config-publisher", cancellationToken);
        return _connection;
    }

    public async Task PublishUpdatedAsync(
        string provider,
        string apiKey,
        bool isActive,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        var evt = new GeminiConfigUpdatedEvent
        {
            Provider = provider,
            ApiKey = apiKey,
            IsActive = isActive,
            UpdatedAt = updatedAt
        };

        var connection = await GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ConfigExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var payload = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(payload);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: _options.ConfigExchange,
            routingKey: _options.GeminiRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Published Gemini config update event to exchange {Exchange} with routing key {RoutingKey}",
            _options.ConfigExchange,
            _options.GeminiRoutingKey);
    }

    public async Task PublishDeletedAsync(string provider, CancellationToken cancellationToken)
    {
        var evt = new GeminiConfigUpdatedEvent
        {
            Provider = provider,
            IsActive = false,
            IsDeleted = true,
            UpdatedAt = DateTime.UtcNow
        };

        var connection = await GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ConfigExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var payload = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(payload);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: _options.ConfigExchange,
            routingKey: _options.GeminiRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Published Gemini config delete event to exchange {Exchange} with routing key {RoutingKey}",
            _options.ConfigExchange,
            _options.GeminiRoutingKey);
    }
}
