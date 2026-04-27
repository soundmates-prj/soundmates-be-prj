namespace AuthService.Domain.Interfaces;

public interface IOutboxRepository
{
    Task EnqueueAsync(string type, object payload, CancellationToken ct = default);
}
