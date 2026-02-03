using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using System.Text.Json;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly LiveSessionDbContext _context;

    public OutboxRepository(LiveSessionDbContext context)
    {
        _context = context;
    }

    public async Task EnqueueAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = eventType,
            Payload = JsonSerializer.Serialize(payload),
            OccurredOnUtc = DateTime.UtcNow,
            ProcessedOnUtc = null,
            RetryCount = 0
        };

        await _context.OutboxMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
