using System;

namespace LiveSessionService.Domain.Entities;

public class PlaylistMedia
{
    public Guid Id { get; set; }
    
    public Guid StationPlaylistId { get; set; }
    
    public string MediaId { get; set; } = null!;
    
    public string SongTitle { get; set; } = null!;
    
    public string? SongArtist { get; set; }
    
    public string? SongAlbum { get; set; }
    
    public string? SongArtUrl { get; set; }
    
    public int DurationSeconds { get; set; }
    
    public string? FilePath { get; set; }
    
    public int PlayCount { get; set; }
    
    public int Weight { get; set; } = 1;
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public virtual StationPlaylist StationPlaylist { get; set; } = null!;
}
