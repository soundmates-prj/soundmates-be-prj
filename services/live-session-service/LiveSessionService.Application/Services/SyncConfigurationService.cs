namespace LiveSessionService.Application.Services;

/// <summary>
/// Configuration for sync operations to prevent DoS and resource exhaustion
/// </summary>
public interface ISyncConfigurationService
{
    int MaxStationsPerSync { get; }
    int MaxMediaFilesPerSync { get; }
    int MaxPlaylistsPerSync { get; }
    int MaxMediaPerPlaylist { get; }
    int BatchSize { get; }
    TimeSpan SyncTimeout { get; }
    TimeSpan MinSyncInterval { get; }
    int MaxRetryAttempts { get; }
    TimeSpan RetryDelay { get; }
}

public sealed class SyncConfigurationService : ISyncConfigurationService
{
    public int MaxStationsPerSync => 100;
    public int MaxMediaFilesPerSync => 10000;
    public int MaxPlaylistsPerSync => 500;
    public int MaxMediaPerPlaylist => 5000;
    public int BatchSize => 100;
    public TimeSpan SyncTimeout => TimeSpan.FromMinutes(30);
    public TimeSpan MinSyncInterval => TimeSpan.FromMinutes(5);
    public int MaxRetryAttempts => 3;
    public TimeSpan RetryDelay => TimeSpan.FromSeconds(5);
}
