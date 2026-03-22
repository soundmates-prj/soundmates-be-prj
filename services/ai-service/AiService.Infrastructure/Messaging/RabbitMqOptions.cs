namespace AiService.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = "rabbitmq";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";

    public string ConfigExchange { get; init; } = "config.exchange";
    public string ConfigQueue { get; init; } = "ai.config.queue";
    public string GeminiRoutingKey { get; init; } = "config.gemini.updated";
}