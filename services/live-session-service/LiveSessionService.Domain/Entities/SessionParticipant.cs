using System;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities;

public class SessionParticipant
{
    public Guid Id { get; set; }
    
    public Guid LiveSessionId { get; set; }
    
    public Guid UserId { get; set; }
    
    public ParticipantRole Role { get; set; }
    
    public DateTime JoinedAt { get; set; }
    
    public DateTime? LeftAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool CanManagePlaylist { get; set; }
    
    public bool CanModerate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public virtual LiveSession LiveSession { get; set; } = null!;
}
