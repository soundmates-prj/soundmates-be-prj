using System;

namespace LiveSessionService.Domain.Entities;

/// <summary>
/// Outbox pattern entity for reliable event publishing
/// Ensures transactional consistency with domain events
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    
    public string Type { get; set; } = null!;
    
    public string Payload { get; set; } = null!;
    
    public DateTime OccurredOnUtc { get; set; }
    
    public DateTime? ProcessedOnUtc { get; set; }
    
    public string? Error { get; set; }
    
    public int RetryCount { get; set; } = 0;
}
