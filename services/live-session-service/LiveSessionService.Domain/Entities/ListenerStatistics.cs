using System;

namespace LiveSessionService.Domain.Entities;

public class ListenerStatistics
{
    public Guid Id { get; set; }
    
    public Guid LiveSessionId { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public int CurrentListeners { get; set; }
    
    public int UniqueListeners { get; set; }
    
    public int PeakListeners { get; set; }
    
    public int TotalConnections { get; set; }
    
    public double AverageListenTimeSeconds { get; set; }
    
    public string? TopCountry { get; set; }
    
    public string? TopCity { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
