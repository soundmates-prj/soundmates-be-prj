namespace LiveSessionService.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = "rabbitmq";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";

    /// <summary>Exchange for config update events (shared with other services).</summary>
    public string ConfigExchange { get; init; } = "config.exchange";

    /// <summary>Queue for live-session-service to consume AzuraCast config events.</summary>
    public string AzuraCastQueue { get; init; } = "livesession.azuracast.config.queue";

    /// <summary>Routing key for AzuraCast config events.</summary>
    public string AzuraCastRoutingKey { get; init; } = "config.azuracast.updated";
}
