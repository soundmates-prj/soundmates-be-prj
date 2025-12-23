using System;

namespace AuthService.Domain.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public DateTime OccurredOnUtc { get; set; }
        public DateTime? ProcessedOnUtc { get; set; }
        public string? Error { get; set; }
    }
}