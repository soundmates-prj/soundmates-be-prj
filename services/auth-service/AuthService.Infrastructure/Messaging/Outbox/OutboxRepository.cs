using System.Text.Json;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;

namespace AuthService.Infrastructure.Messaging.Outbox;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly AuthDbContext _db;

    public OutboxRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task EnqueueAsync(string type, object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = json,
            OccurredOnUtc = DateTime.UtcNow,
            ProcessedOnUtc = null,
            Error = null
        };

        _db.OutboxMessages.Add(msg);
        await _db.SaveChangesAsync(ct);
    }
}
