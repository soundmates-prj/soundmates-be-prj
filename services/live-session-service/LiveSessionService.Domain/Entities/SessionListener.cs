using System;

namespace LiveSessionService.Domain.Entities;

public class SessionListener
{
    public Guid Id { get; set; }
    
    public Guid LiveSessionId { get; set; }
    
    public Guid? UserId { get; set; }
    
    public string? AnonymousIdentifier { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    public string? Country { get; set; }
    
    public string? City { get; set; }
    
    public DateTime ConnectedAt { get; set; }
    
    public DateTime? DisconnectedAt { get; set; }
    
    public bool IsConnected { get; set; } = true;
    
    public int DurationSeconds { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public virtual LiveSession LiveSession { get; set; } = null!;
}
