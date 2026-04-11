using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.LiveSessions.Scheduling;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;

public sealed class GetLiveSessionHandler : IQueryHandler<GetLiveSessionQuery, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IMediaFileRepository _mediaFileRepository;

    public GetLiveSessionHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository,
        IDateTimeProvider dateTimeProvider,
        IAzuraCastClient azuraCastClient,
        IMediaFileRepository mediaFileRepository)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
        _dateTimeProvider = dateTimeProvider;
        _azuraCastClient = azuraCastClient;
        _mediaFileRepository = mediaFileRepository;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        GetLiveSessionQuery query,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithStationAsync(query.SessionId, cancellationToken);

        if (session == null)
        {
            return Result<LiveSessionResult>.Failure("Session not found", ErrorCode.NotFound);
        }

        DateTime? scheduleStartAt = null;
        if (session.Status == SessionStatus.Scheduled)
        {
            var schedules = await _scheduleRepository.GetByLiveSessionIdAsync(session.Id, cancellationToken);
            scheduleStartAt = ScheduleOccurrenceCalculator.GetNextOccurrenceUtc(schedules, _dateTimeProvider.UtcNow);
        }

        // Fetch now playing data from AzuraCast if session has a station
        NowPlayingTrackResult? currentTrack = null;
        NowPlayingTrackResult? playingNext = null;
        List<NowPlayingTrackResult> songHistory = [];

        if (session.AzuraCastStation != null)
        {
            var nowPlayingData = await _azuraCastClient.GetNowPlayingAsync(
                session.AzuraCastStation.ExternalStationId, cancellationToken);

            if (nowPlayingData != null)
            {
                currentTrack = nowPlayingData.NowPlaying != null ? await MapTrackAsync(nowPlayingData.NowPlaying, cancellationToken) : null;
                playingNext = nowPlayingData.PlayingNext != null ? await MapTrackAsync(nowPlayingData.PlayingNext, cancellationToken) : null;
                songHistory = nowPlayingData.SongHistory?
                    .Select(MapHistoryTrack)
                    .ToList() ?? [];
            }
        }

        var nowPlaying = currentTrack != null ? new StationNowPlayingResult
        {
            ExternalStationId = session.AzuraCastStation?.ExternalStationId ?? 0,
            StationName = session.AzuraCastStation?.StationName ?? "",
            StationShortcode = session.AzuraCastStation?.StationShortcode,
            ListenUrl = session.AzuraCastStation?.StreamUrl,
            PublicPlayerUrl = session.AzuraCastStation?.PublicPlayerUrl,
            CurrentTrack = currentTrack,
            PlayingNext = playingNext,
            SongHistory = songHistory
        } : null;

        var result = new LiveSessionResult
        {
            Id = session.Id,
            UserId = session.HostUserId,
            StationId = session.AzuraCastStationId!.Value,
            StationName = session.AzuraCastStation?.StationName,
            SessionName = session.SessionName,
            Description = session.Description,
            Status = session.Status.ToString(),
            ScheduledStartAt = session.Status == SessionStatus.Scheduled
                ? scheduleStartAt ?? session.StartedAt
                : null,
            StartedAt = session.Status is SessionStatus.Live or SessionStatus.Paused or SessionStatus.Ended
                ? session.StartedAt
                : null,
            EndedAt = session.Status == SessionStatus.Ended
                ? session.EndedAt
                : null,
            TotalListeners = session.Listeners
                .Select(l => l.UserId.HasValue
                    ? $"u:{l.UserId.Value}"
                    : $"a:{l.AnonymousIdentifier ?? l.Id.ToString()}")
                .Distinct()
                .Count(),
            PeakListeners = session.Listeners.Count(l => l.IsConnected),
            ListenersCount = session.Listeners.Count(l => l.IsConnected),
            StreamUrl = session.AzuraCastStation?.StreamUrl,
            StationShortcode = session.AzuraCastStation?.StationShortcode,
            PublicPlayerUrl = session.AzuraCastStation?.PublicPlayerUrl,
            ThumbnailUrl = session.ThumbnailUrl,
            Genre = session.Genre,
            CreatedAt = session.CreatedAt,
            NowPlaying = nowPlaying
        };

        return Result<LiveSessionResult>.Success(result);
    }

    private async Task<NowPlayingTrackResult> MapTrackAsync(AzuraCastCurrentSongData track, CancellationToken cancellationToken)
    {
        var lyrics = track.Song?.Lyrics;
        var artUrl = track.Song?.Art;
        
        // If AzuraCast doesn't have lyrics, try to get them from our DB
        // Also fallback for missing artwork
        if ((string.IsNullOrWhiteSpace(lyrics) || string.IsNullOrWhiteSpace(artUrl) || artUrl.Contains("generic_song")))
        {
            MediaFile? mediaFile = null;

            // 1. Try to match by SongId (which usually matches our UniqueId / AzuraCastMediaId)
            if (!string.IsNullOrWhiteSpace(track.Song?.Id))
            {
                mediaFile = await _mediaFileRepository.GetByAzuraCastMediaIdAsync(track.Song.Id, cancellationToken);
            }

            // 2. Try to match by title and artist if SongId fails or is missing
            if (mediaFile == null && !string.IsNullOrWhiteSpace(track.Song?.Title))
            {
                var files = await _mediaFileRepository.GetAllAsync(cancellationToken);
                mediaFile = files.FirstOrDefault(f => 
                    string.Equals(f.Title, track.Song.Title, StringComparison.OrdinalIgnoreCase) && 
                    (string.IsNullOrWhiteSpace(track.Song.Artist) || string.Equals(f.Artist, track.Song.Artist, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (mediaFile != null)
            {
                if (string.IsNullOrWhiteSpace(lyrics) && !string.IsNullOrWhiteSpace(mediaFile.Lyrics))
                {
                    lyrics = mediaFile.Lyrics;
                }
                
                if ((string.IsNullOrWhiteSpace(artUrl) || artUrl.Contains("generic_song")) 
                    && !string.IsNullOrWhiteSpace(mediaFile.ArtUrl))
                {
                    artUrl = mediaFile.ArtUrl;
                }
            }
        }

        artUrl = NormalizeArtworkUrl(artUrl);

        return new NowPlayingTrackResult
        {
            ShId = track.ShId,
            Text = track.Song?.Text,
            Title = track.Song?.Title,
            Artist = track.Song?.Artist,
            Album = track.Song?.Album,
            Genre = track.Song?.Genre,
            ArtUrl = artUrl,
            Lyrics = lyrics,
            PlayedAt = track.PlayedAt,
            Duration = track.Duration,
            Elapsed = track.Elapsed,
            Remaining = track.Remaining,
            IsRequest = track.IsRequest
        };
    }

    private static NowPlayingTrackResult MapHistoryTrack(AzuraCastSongHistoryData h)
        => new()
        {
            ShId = h.ShId,
            Text = h.Song?.Text,
            Title = h.Song?.Title,
            Artist = h.Song?.Artist,
            Album = h.Song?.Album,
            Genre = h.Song?.Genre,
            ArtUrl = NormalizeArtworkUrl(h.Song?.Art),
            Lyrics = h.Song?.Lyrics,
            PlayedAt = h.PlayedAt
        };

    private static string? NormalizeArtworkUrl(string? artUrl)
    {
        if (string.IsNullOrWhiteSpace(artUrl))
            return artUrl;

        if (!Uri.TryCreate(artUrl, UriKind.Absolute, out var uri))
            return artUrl;

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return artUrl;

        // Domain public should always be served via HTTPS.
        if (uri.Host.EndsWith("soundmates.xyz", StringComparison.OrdinalIgnoreCase))
            return BuildHttpsUrl(uri);

        // AzuraCast art endpoint often comes from internal HTTP base; prefer radio domain when available.
        if (LooksLikeInternalUrl(uri) || uri.Port == 5000)
        {
            var publicRadioBase = Environment.GetEnvironmentVariable("AZURACAST_PUBLIC_BASE_URL");

            if (Uri.TryCreate(publicRadioBase, UriKind.Absolute, out var publicBaseUri))
            {
                var rebuilt = new UriBuilder(publicBaseUri)
                {
                    Path = uri.AbsolutePath,
                    Query = uri.Query.TrimStart('?')
                };

                return rebuilt.Uri.ToString();
            }
        }

        return artUrl;
    }

    private static bool LooksLikeInternalUrl(Uri uri)
        => uri.IsLoopback
            || uri.Host.Equals("host.docker.internal", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase);

    private static string BuildHttpsUrl(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Scheme = Uri.UriSchemeHttps,
            Port = uri.IsDefaultPort ? -1 : uri.Port
        };

        return builder.Uri.ToString();
    }
}
