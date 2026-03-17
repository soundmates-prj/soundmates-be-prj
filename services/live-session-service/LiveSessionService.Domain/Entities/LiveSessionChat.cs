namespace LiveSessionService.Domain.Entities
{
    public partial class LiveSessionChat
    {
        public Guid Id { get; set; }

        public string Message { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public Guid UserId { get; set; }

        public Guid LiveSessionId { get; set; }

        public virtual LiveSession LiveSession { get; set; } = null!;
    }
}
