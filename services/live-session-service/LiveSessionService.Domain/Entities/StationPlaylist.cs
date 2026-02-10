using System;
using System.Collections.Generic;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities;

public class StationPlaylist
{
    public Guid Id { get; set; }
    
    public Guid AzuraCastStationId { get; set; }
    
    public int ExternalPlaylistId { get; set; }
    
    public string PlaylistName { get; set; } = null!;
    
    public PlaylistType Type { get; set; }
    
    public PlaylistSource Source { get; set; }
    
    public int PlaylistOrder { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public bool IncludeInRequests { get; set; }
    
    public bool IncludeInOnDemand { get; set; }
    
    public int Weight { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? LastSyncedAt { get; set; }
    
    public virtual AzuraCastStation AzuraCastStation { get; set; } = null!;
    
    public virtual ICollection<PlaylistMedia> Media { get; set; } = new List<PlaylistMedia>();
}
