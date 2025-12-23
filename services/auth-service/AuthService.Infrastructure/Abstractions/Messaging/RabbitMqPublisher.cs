using System.Text;
using AuthService.Application.Abstractions.Messaging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace AuthService.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessageBusPublisher
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    public RabbitMqPublisher(IConfiguration cfg)
    {
        var cs = cfg.GetConnectionString("RabbitMq");
        if (!string.IsNullOrWhiteSpace(cs))
        {
            _factory = new ConnectionFactory { Uri = new Uri(cs) };
        }
        else
        {
            _factory = new ConnectionFactory
            {
                HostName = cfg["RabbitMq:HostName"] ?? "rabbitmq",
                Port = int.TryParse(cfg["RabbitMq:Port"], out var p) ? p : 5672,
                UserName = cfg["RabbitMq:UserName"] ?? "guest",
                Password = cfg["RabbitMq:Password"] ?? "guest",
                VirtualHost = cfg["RabbitMq:VirtualHost"] ?? "/"
            };
        }
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true }) return _connection;
        _connection = await _factory.CreateConnectionAsync(clientProvidedName: "auth-service", cancellationToken: ct);
        return _connection;
    }

    public async Task PublishAsync(string type, string payload, CancellationToken ct = default)
    {
        const string exchange = "auth.users";
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
    }
}