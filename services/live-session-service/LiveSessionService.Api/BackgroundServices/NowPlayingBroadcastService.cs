using LiveSessionService.Api.Hubs;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Application.Features.Stations.Queries.GetStationNowPlaying;
using LiveSessionService.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LiveSessionService.Api.BackgroundServices;

/// <summary>
/// Polls AzuraCast for each enabled station on a fixed interval.
/// Broadcasts a "NowPlayingUpdated" event via SignalR only when the current song changes (ShId diff).
/// Clients subscribe by calling hub method: JoinStation("{station-guid}")
/// </summary>
public sealed class NowPlayingBroadcastService : BackgroundService
{
    private readonly IHubContext<NowPlayingHub> _hub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NowPlayingBroadcastService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(15);

    // Track last broadcast ShId per station Guid to avoid redundant pushes
    private readonly Dictionary<Guid, long> _lastShId = new();

    public NowPlayingBroadcastService(
        IHubContext<NowPlayingHub> hub,
        IServiceScopeFactory scopeFactory,
        ILogger<NowPlayingBroadcastService> logger)
    {
        _hub = hub;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NowPlayingBroadcastService started � polling every {Interval}s",
            _pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await PollAndBroadcastAsync(stoppingToken);
            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task PollAndBroadcastAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var stationRepo = scope.ServiceProvider.GetRequiredService<IAzuraCastStationRepository>();
            var sessionRepo = scope.ServiceProvider.GetRequiredService<ILiveSessionRepository>();
            var queries     = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var stations = await stationRepo.GetAllEnabledAsync(ct);
            var activeSessions = await sessionRepo.GetActiveSessionsAsync(ct);

            foreach (var station in stations)
            {
                try
                {
                    var result = await queries.Send<GetStationNowPlayingQuery, StationNowPlayingResult>(
                        new GetStationNowPlayingQuery(station.Id), ct);

                    if (!result.IsSuccess || result.Data == null) continue;

                    var nowPlaying  = result.Data;
                    var currentShId = nowPlaying.CurrentTrack?.ShId ?? 0;

                    // Skip broadcast if song hasn't changed
                    if (_lastShId.TryGetValue(station.Id, out var lastShId) && lastShId == currentShId)
                        continue;

                    _lastShId[station.Id] = currentShId;

                    await _hub.Clients
                        .Group($"station-{station.Id}")
                        .SendAsync("NowPlayingUpdated", nowPlaying, ct);

                    var relatedSessions = activeSessions.Where(s => s.AzuraCastStationId == station.Id).ToList();
                    foreach (var session in relatedSessions)
                    {
                        await _hub.Clients
                            .Group($"session-{session.Id}")
                            .SendAsync("NowPlayingUpdated", nowPlaying, ct);
                    }

                    _logger.LogDebug(
                        "Broadcast NowPlaying for station {StationId}: [{ShId}] {Title} � {Artist}",
                        station.Id, currentShId,
                        nowPlaying.CurrentTrack?.Title ?? "-",
                        nowPlaying.CurrentTrack?.Artist ?? "-");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to poll/broadcast now-playing for station {StationId}",
                        station.Id);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unhandled error in NowPlayingBroadcastService");
        }
    }
}
