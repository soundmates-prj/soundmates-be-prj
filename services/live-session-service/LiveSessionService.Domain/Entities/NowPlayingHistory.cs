using System;

namespace LiveSessionService.Domain.Entities;

public partial class NowPlayingHistory
{
    public Guid Id { get; set; }
    
    public Guid LiveSessionId { get; set; }
    
    public long? AzuraCastSongHistoryId { get; set; }
    
    public string SongTitle { get; set; } = null!;
    
    public string? SongArtist { get; set; }
    
    public string? SongAlbum { get; set; }
    
    public string? SongArtUrl { get; set; }
    
    //public string? Lyrics { get; set; }

    public int DurationSeconds { get; set; }
    
    public DateTime PlayedAt { get; set; }
    
    public DateTime? EndedAt { get; set; }
    
    public int ListenerPeak { get; set; }
    
    public int ListenerCount { get; set; }
    
    public bool IsRequest { get; set; }
    
    public Guid? RequestedByUserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public virtual LiveSession LiveSession { get; set; } = null!;
}
