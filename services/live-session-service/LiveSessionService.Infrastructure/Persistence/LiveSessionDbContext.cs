using Microsoft.EntityFrameworkCore;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence;

public class LiveSessionDbContext : DbContext
{
    public LiveSessionDbContext(DbContextOptions<LiveSessionDbContext> options)
        : base(options)
    {
    }

    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
    public DbSet<AzuraCastStation> AzuraCastStations => Set<AzuraCastStation>();
    public DbSet<NowPlayingHistory> NowPlayingHistory => Set<NowPlayingHistory>();
    public DbSet<SessionParticipant> SessionParticipants => Set<SessionParticipant>();
    public DbSet<SessionActivity> SessionActivities => Set<SessionActivity>();
    public DbSet<SessionListener> SessionListeners => Set<SessionListener>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<StationPlaylist> StationPlaylists => Set<StationPlaylist>();
    public DbSet<PlaylistMedia> PlaylistMedias => Set<PlaylistMedia>();
    public DbSet<StationMount> StationMounts => Set<StationMount>();
    public DbSet<ListenerStatistics> ListenerStatistics => Set<ListenerStatistics>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<LiveSessionChat> LiveSessionChats => Set<LiveSessionChat>();
    public DbSet<SessionSchedule> SessionSchedules => Set<SessionSchedule>();
    public DbSet<SongRequest> SongRequests => Set<SongRequest>();
    public DbSet<UserPlaylist> UserPlaylists => Set<UserPlaylist>();
    public DbSet<UserPlaylistMedia> UserPlaylistMedias => Set<UserPlaylistMedia>();
    public DbSet<Podcast> Podcasts => Set<Podcast>();
    public DbSet<PodcastEpisode> PodcastEpisodes => Set<PodcastEpisode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LiveSessionDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
