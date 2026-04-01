using System.Text;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace LiveSessionService.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessageBusPublisher
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;

    public RabbitMqPublisher(IConfiguration cfg, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        
        // Read from environment variables first (Docker/Kubernetes), then from config
        var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") 
                    ?? cfg["RabbitMq:HostName"]?.Replace("${RABBITMQ_HOST}", "")?.Trim()
                    ?? cfg["RABBITMQ_HOST"] 
                    ?? "rabbitmq";
        
        if (hostName.Contains("${"))
        {
            hostName = "rabbitmq";
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
            RequestedConnectionTimeout = TimeSpan.FromSeconds(30)
        };
        
        _logger.LogInformation("RabbitMQ Publisher Configuration: Host={Host}, Port={Port}, VHost={VHost}", 
            _factory.HostName, _factory.Port, _factory.VirtualHost);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        try
        {
            _logger.LogInformation("Creating RabbitMQ connection to {Host}:{Port}",
                _factory.HostName, _factory.Port);

            _connection = await _factory.CreateConnectionAsync(
                clientProvidedName: "live-session-service-publisher",
                cancellationToken: ct);

            _logger.LogInformation("RabbitMQ connection established");
            return _connection;
        }
        catch (BrokerUnreachableException ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ broker");
            throw;
        }
    }

    public async Task PublishAsync(string type, string payload, CancellationToken ct = default)
    {
        const string exchange = "livesession.events";

        try
        {
            var conn = await GetConnectionAsync(ct);
            await using var channel = await conn.CreateChannelAsync(cancellationToken: ct);

            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            var body = Encoding.UTF8.GetBytes(payload);
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: type,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            _logger.LogInformation("Published message to RabbitMQ: {Type}", type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to RabbitMQ: {Type}", type);
            throw;
        }
    }
}
