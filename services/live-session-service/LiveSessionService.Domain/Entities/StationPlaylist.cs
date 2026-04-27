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

    public SongPlaybackOrder SongPlaybackOrder { get; set; } = SongPlaybackOrder.Sequential;
    
    public int PlaylistOrder { get; set; }
    
    public bool IsEnabled { get; set; } = true;

    public string? Description  { get; set; }

    public bool IncludeInRequests { get; set; }
    
    public bool IncludeInOnDemand { get; set; }
    
    public int Weight { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? LastSyncedAt { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    
    public DateTime? DeletedAt { get; set; }
    
    public Guid? DeletedBy { get; set; }
    
    public virtual AzuraCastStation AzuraCastStation { get; set; } = null!;
    
    public virtual ICollection<PlaylistMedia> Media { get; set; } = new List<PlaylistMedia>();
}
