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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LiveSessionDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
