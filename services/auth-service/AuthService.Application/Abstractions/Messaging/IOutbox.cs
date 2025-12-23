namespace AuthService.Application.Abstractions.Messaging;

public interface IOutbox
{
    Task EnqueueAsync(string type, object payload, CancellationToken ct = default);
}