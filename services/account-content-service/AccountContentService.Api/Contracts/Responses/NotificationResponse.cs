namespace AccountContentService.Api.Contracts.Responses
{
    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public Guid ReferenceId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
