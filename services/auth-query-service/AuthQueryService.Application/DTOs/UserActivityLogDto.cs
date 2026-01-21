namespace AuthQueryService.Application.DTOs
{
    public sealed class UserActivityLogDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? Reason { get; set; }
        public int? ErrorCode { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Location { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
