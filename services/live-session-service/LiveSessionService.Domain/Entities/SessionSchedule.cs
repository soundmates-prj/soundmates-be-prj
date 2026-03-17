namespace LiveSessionService.Domain.Entities
{
    public partial class SessionSchedule
    {
        public Guid Id { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Title { get; set; } = null!;

        public string? Status { get; set; }

        public Guid LiveSessionId { get; set; }

        public virtual LiveSession LiveSession { get; set; } = null!;
    }
}
