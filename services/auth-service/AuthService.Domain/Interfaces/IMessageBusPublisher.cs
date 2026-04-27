namespace AuthService.Domain.Interfaces;

public interface IMessageBusPublisher
{
    Task PublishAsync(string type, string payload, CancellationToken ct = default);
}
