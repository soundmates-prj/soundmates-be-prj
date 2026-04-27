using System;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities;

public class SessionActivity
{
    public Guid Id { get; set; }
    
    public Guid LiveSessionId { get; set; }
    
    public Guid? UserId { get; set; }
    
    public ActivityType ActivityType { get; set; }
    
    public string? ActivityData { get; set; }
    
    public string? Message { get; set; }
    
    public DateTime OccurredAt { get; set; }
    
    public virtual LiveSession LiveSession { get; set; } = null!;
}
