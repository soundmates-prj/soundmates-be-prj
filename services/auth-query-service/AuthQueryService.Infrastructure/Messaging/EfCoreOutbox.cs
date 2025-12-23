using System.Text.Json;
using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Domain.Entities;
using AuthQueryService.Infrastructure.Data;

namespace AuthQueryService.Infrastructure.Messaging;

public sealed class EfCoreOutbox(AuthDbContext db) : IOutbox
{
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

        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync(ct);
    }
}