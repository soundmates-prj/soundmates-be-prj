using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.RegularExpressions;

namespace LiveSessionService.Infrastructure.Persistence;

public class LiveSessionDbContext : DbContext
{
    private readonly IConfiguration? _configuration;

    public LiveSessionDbContext()
    {
    }

    public LiveSessionDbContext(DbContextOptions<LiveSessionDbContext> options)
        : base(options)
    {
    }

    public LiveSessionDbContext(DbContextOptions<LiveSessionDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var config = _configuration ?? new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = ResolveConnectionString(config);
            optionsBuilder.UseNpgsql(connectionString);
        }
        
        // Suppress pending model changes warning to allow migrations to be applied
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    private static string ResolveConnectionString(IConfiguration config)
    {
        var raw = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }

        return Regex.Replace(raw, "\\$\\{(?<key>[A-Za-z0-9_]+)\\}", match =>
        {
            var key = match.Groups["key"].Value;
            var value = Environment.GetEnvironmentVariable(key) ?? config[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Missing environment variable '{key}' required for connection string.");
            }
            return value;
        });
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
    public DbSet<UserSavedPodcast> UserSavedPodcasts => Set<UserSavedPodcast>();
    public DbSet<PodcastRequest> PodcastRequests => Set<PodcastRequest>();
    public DbSet<StationMediaFile> StationMediaFiles => Set<StationMediaFile>();
    public DbSet<SyncAuditLog> SyncAuditLogs => Set<SyncAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LiveSessionDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
