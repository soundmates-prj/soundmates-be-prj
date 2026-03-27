using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities
{
    public partial class SessionSchedule
    {
        public Guid Id { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }

        public string? Title { get; set; }

        public ScheduleStatus Status { get; set; }

        public bool IsRecurring { get; set; }

        public DaysOfWeek DaysOfWeek { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public Guid? CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }

        public Guid LiveSessionId { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual LiveSession LiveSession { get; set; } = null!;
    }
}
