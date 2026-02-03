using System;
using System.Collections.Generic;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities;

public partial class LiveSession
{
    public Guid Id { get; set; }
    
    public Guid HostUserId { get; set; }
    
    public Guid? AzuraCastStationId { get; set; }
    
    public string SessionName { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public SessionStatus Status { get; set; }
    
    public DateTime StartedAt { get; set; }
    
    public DateTime? EndedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public int MaxListeners { get; set; } = 100;
    
    public bool IsPublic { get; set; } = true;
    
    public string? ThumbnailUrl { get; set; }
    
    public string? Genre { get; set; }
    
    public virtual AzuraCastStation? AzuraCastStation { get; set; }
    
    public virtual ICollection<SessionParticipant> Participants { get; set; } = new List<SessionParticipant>();
    
    public virtual ICollection<SessionActivity> Activities { get; set; } = new List<SessionActivity>();
    
    public virtual ICollection<NowPlayingHistory> NowPlayingHistory { get; set; } = new List<NowPlayingHistory>();
    
    public virtual ICollection<SessionListener> Listeners { get; set; } = new List<SessionListener>();
}
