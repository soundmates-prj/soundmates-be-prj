using System;
using System.Collections.Generic;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities;

public partial class AzuraCastStation
{
    public Guid Id { get; set; }
    
    public int ExternalStationId { get; set; }
    
    public string StationName { get; set; } = null!;
    
    public string? StationShortcode { get; set; }
    
    public string? Description { get; set; }
    
    public string StreamUrl { get; set; } = null!;
    
    public string? PublicPlayerUrl { get; set; }
    
    public string? MountPoint { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public string? ApiBaseUrl { get; set; }
    
    public string? ApiKey { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? LastSyncedAt { get; set; }
    
    public StationSyncStatus SyncStatus { get; set; }
    
    public string? LastSyncError { get; set; }
    
    public virtual ICollection<LiveSession> LiveSessions { get; set; } = new List<LiveSession>();
    
    public virtual ICollection<StationPlaylist> Playlists { get; set; } = new List<StationPlaylist>();
    
    public virtual ICollection<StationMount> Mounts { get; set; } = new List<StationMount>();
}