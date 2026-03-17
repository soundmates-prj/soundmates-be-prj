using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities
{
    public partial class SongRequest
    {
        public Guid Id { get; set; }

        public Guid LiveSessionId { get; set; }

        public Guid MediaFileId { get; set; }

        public Guid RequestedByUserId { get; set; }

        public SongRequestStatus Status { get; set; }

        public Guid? ReviewedByUserId { get; set; }

        public DateTime RequestedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }
        public string? Message { get; set; }

        public string? RejectReason { get; set; }

        public virtual LiveSession LiveSession { get; set; } = null!;

        public virtual MediaFile MediaFile { get; set; } = null!;
    }
}
