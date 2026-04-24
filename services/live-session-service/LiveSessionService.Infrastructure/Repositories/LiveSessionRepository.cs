using Microsoft.EntityFrameworkCore;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Domain.Models;
using LiveSessionService.Infrastructure.Persistence;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class LiveSessionRepository : ILiveSessionRepository
{
    private readonly LiveSessionDbContext _context;

    public LiveSessionRepository(LiveSessionDbContext context)
    {
        _context = context;
    }

    public async Task<LiveSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LiveSessions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<LiveSession?> GetByIdWithStationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LiveSessions
            .Include(x => x.AzuraCastStation)
            .Include(x => x.Listeners)
            .Include(x => x.Chats)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<LiveSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LiveSessions
            .Include(x => x.AzuraCastStation)
            .Include(x => x.Listeners)
            .Where(x => x.Status == Domain.Enums.SessionStatus.Live)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LiveSession>> GetByHostUserIdAsync(Guid hostUserId, CancellationToken cancellationToken = default)
    {
        return await _context.LiveSessions
            .Where(x => x.HostUserId == hostUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LiveSession>> GetAllWithStationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LiveSessions
            .Include(x => x.AzuraCastStation)
            .Include(x => x.Listeners)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LiveSession session, CancellationToken cancellationToken = default)
    {
        await _context.LiveSessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LiveSession session, CancellationToken cancellationToken = default)
    {
        _context.LiveSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await GetByIdAsync(id, cancellationToken);
        if (session != null)
        {
            _context.LiveSessions.Remove(session);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetActiveSessionCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LiveSessions
            .CountAsync(x => x.Status == Domain.Enums.SessionStatus.Live, cancellationToken);
    }

    public async Task<StaffDashboardOverview> GetStaffDashboardOverviewAsync(int days, CancellationToken cancellationToken = default)
    {
        var normalizedDays = days <= 0 ? 7 : Math.Min(days, 90);
        var utcToday = DateTime.UtcNow.Date;
        var fromDate = utcToday.AddDays(-(normalizedDays - 1));

        var totalSessions = await _context.LiveSessions.CountAsync(cancellationToken);
        var liveSessions = await _context.LiveSessions.CountAsync(
            x => x.Status == Domain.Enums.SessionStatus.Live,
            cancellationToken);

        var listenerRows = await _context.SessionListeners
            .AsNoTracking()
            .Where(x => x.ConnectedAt >= fromDate)
            .Select(x => new { x.Id, x.ConnectedAt, x.UserId, x.AnonymousIdentifier })
            .ToListAsync(cancellationToken);

        string ToListenerKey(Guid id, Guid? userId, string? anonymousIdentifier)
        {
            if (userId.HasValue)
            {
                return $"u:{userId.Value}";
            }

            if (!string.IsNullOrWhiteSpace(anonymousIdentifier))
            {
                return $"a:{anonymousIdentifier}";
            }

            return $"g:{id}";
        }

        var dailyListeners = Enumerable.Range(0, normalizedDays)
            .Select(offset => fromDate.AddDays(offset))
            .Select(day =>
            {
                var uniqueCount = listenerRows
                    .Where(x => x.ConnectedAt.Date == day)
                    .Select(x => ToListenerKey(x.Id, x.UserId, x.AnonymousIdentifier))
                    .Distinct()
                    .Count();

                return new DailyListenerMetric
                {
                    Date = day,
                    ListenerCount = uniqueCount
                };
            })
            .ToList();

        var listenersToday = dailyListeners
            .FirstOrDefault(x => x.Date == utcToday)
            ?.ListenerCount ?? 0;

        return new StaffDashboardOverview
        {
            TotalSessions = totalSessions,
            LiveSessions = liveSessions,
            ListenersToday = listenersToday,
            DailyListeners = dailyListeners
        };
    }

    public async Task EndSessionCleanupAsync(Guid sessionId, DateTime endedAt, CancellationToken cancellationToken = default)
    {
        var listeners = await _context.SessionListeners
            .Where(x => x.LiveSessionId == sessionId && x.IsConnected)
            .ToListAsync(cancellationToken);

        foreach (var listener in listeners)
        {
            listener.IsConnected = false;
            listener.DisconnectedAt = endedAt;
            listener.UpdatedAt = endedAt;
            listener.DurationSeconds = Math.Max(0, (int)(endedAt - listener.ConnectedAt).TotalSeconds);
        }

        if (listeners.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
    public async Task<HostDashboardOverview> GetHostDashboardOverviewAsync(Guid hostUserId, int days, CancellationToken cancellationToken = default)
    {
        var normalizedDays = days <= 0 ? 7 : Math.Min(days, 90);
        var utcToday = DateTime.UtcNow.Date;
        var fromDate = utcToday.AddDays(-(normalizedDays - 1));

        // Get all sessions of the host
        var hostSessions = await _context.LiveSessions
            .AsNoTracking()
            .Where(x => x.HostUserId == hostUserId)
            .ToListAsync(cancellationToken);

        var hostSessionIds = hostSessions.Select(x => x.Id).ToList();

        // 1. Total Sessions
        var totalSessions = hostSessions.Count;

        // 2. Total Listeners & Chart Data
        var listenerRows = new List<SessionListener>();
        if (hostSessionIds.Any())
        {
            listenerRows = await _context.SessionListeners
                .AsNoTracking()
                .Where(x => hostSessionIds.Contains(x.LiveSessionId) && x.ConnectedAt >= fromDate)
                .Select(x => new { x.Id, x.LiveSessionId, x.ConnectedAt, x.UserId, x.AnonymousIdentifier })
                .ToListAsync(cancellationToken)
                .ContinueWith(t => t.Result.Select(x => new SessionListener 
                { 
                    Id = x.Id, 
                    LiveSessionId = x.LiveSessionId, 
                    ConnectedAt = x.ConnectedAt, 
                    UserId = x.UserId, 
                    AnonymousIdentifier = x.AnonymousIdentifier 
                }).ToList(), cancellationToken);
        }

        string ToListenerKey(Guid id, Guid? userId, string? anonymousIdentifier)
        {
            if (userId.HasValue) return $"u:{userId.Value}";
            if (!string.IsNullOrWhiteSpace(anonymousIdentifier)) return $"a:{anonymousIdentifier}";
            return $"g:{id}";
        }

        var uniqueListeners = listenerRows
            .Select(x => ToListenerKey(x.Id, x.UserId, x.AnonymousIdentifier))
            .Distinct()
            .Count();

        var totalListeners = uniqueListeners;

        var chartData = Enumerable.Range(0, normalizedDays)
            .Select(offset => fromDate.AddDays(offset))
            .Select(day =>
            {
                var sessionsThatDay = hostSessions.Count(x => x.StartedAt.HasValue && x.StartedAt.Value.Date == day);
                var uniqueListenersThatDay = listenerRows
                    .Where(x => x.ConnectedAt.Date == day)
                    .Select(x => ToListenerKey(x.Id, x.UserId, x.AnonymousIdentifier))
                    .Distinct()
                    .Count();

                return new DailyHostStatMetric
                {
                    Date = day,
                    SessionsCount = sessionsThatDay,
                    ListenersCount = uniqueListenersThatDay
                };
            })
            .ToList();

        // 3. Pending Music Requests
        var pendingMusicRequests = 0;
        if (hostSessionIds.Any())
        {
            pendingMusicRequests = await _context.SongRequests
                .CountAsync(x => hostSessionIds.Contains(x.LiveSessionId) && x.Status == Domain.Enums.SongRequestStatus.Pending, cancellationToken);
        }

        // 4. Ended Sessions Analysis
        var endedSessions = hostSessions
            .Where(x => x.Status == Domain.Enums.SessionStatus.Ended)
            .OrderByDescending(x => x.EndedAt)
            .Take(10) // Limit to last 10 ended sessions for performance
            .ToList();

        var endedSessionIds = endedSessions.Select(x => x.Id).ToList();
        var musicRequestsBySession = new Dictionary<Guid, int>();
        
        if (endedSessionIds.Any())
        {
            musicRequestsBySession = await _context.SongRequests
                .Where(x => endedSessionIds.Contains(x.LiveSessionId))
                .GroupBy(x => x.LiveSessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SessionId, x => x.Count, cancellationToken);
        }

        var endedSessionsAnalysis = endedSessions.Select(s => 
        {
            var durationSpan = s.EndedAt.HasValue && s.StartedAt.HasValue 
                ? s.EndedAt.Value - s.StartedAt.Value 
                : TimeSpan.Zero;
            
            var sessionUniqueListeners = listenerRows
                .Where(x => x.LiveSessionId == s.Id)
                .Select(x => ToListenerKey(x.Id, x.UserId, x.AnonymousIdentifier))
                .Distinct()
                .Count();

            return new EndedSessionAnalysis
            {
                SessionId = s.Id,
                SessionName = s.SessionName,
                EndedAt = s.EndedAt,
                TotalDurationMinutes = durationSpan.TotalMinutes,
                TotalListeners = sessionUniqueListeners,
                MusicRequestsCount = musicRequestsBySession.ContainsKey(s.Id) ? musicRequestsBySession[s.Id] : 0
            };
        }).ToList();

        // 5. Recent Music Requests
        var recentMusicRequests = new List<SongRequest>();
        if (hostSessionIds.Any())
        {
            recentMusicRequests = await _context.SongRequests
                .Include(x => x.MediaFile)
                .Where(x => hostSessionIds.Contains(x.LiveSessionId))
                .OrderByDescending(x => x.RequestedAt)
                .Take(5)
                .ToListAsync(cancellationToken);
        }

        return new HostDashboardOverview
        {
            TotalSessions = totalSessions,
            TotalListeners = totalListeners,
            PendingMusicRequests = pendingMusicRequests,
            ChartData = chartData,
            RecentMusicRequests = recentMusicRequests,
            EndedSessionsAnalysis = endedSessionsAnalysis
        };
    }

    public async Task<HostAnalyticsOverview> GetHostAnalyticsOverviewAsync(Guid hostUserId, int days, CancellationToken cancellationToken = default)
    {
        var normalizedDays = days <= 0 ? 7 : Math.Min(days, 90);
        var utcToday = DateTime.UtcNow.Date;
        var fromDate = utcToday.AddDays(-(normalizedDays - 1));

        // Get all sessions of the host
        var hostSessions = await _context.LiveSessions
            .AsNoTracking()
            .Where(x => x.HostUserId == hostUserId)
            .ToListAsync(cancellationToken);

        var hostSessionIds = hostSessions.Select(x => x.Id).ToList();

        // Total Sessions
        var totalSessions = hostSessions.Count;

        // Fetch Listener records
        var listenerRows = new List<SessionListener>();
        if (hostSessionIds.Any())
        {
            listenerRows = await _context.SessionListeners
                .AsNoTracking()
                .Where(x => hostSessionIds.Contains(x.LiveSessionId) && x.ConnectedAt >= fromDate)
                .Select(x => new { x.Id, x.LiveSessionId, x.ConnectedAt, x.UserId, x.AnonymousIdentifier })
                .ToListAsync(cancellationToken)
                .ContinueWith(t => t.Result.Select(x => new SessionListener 
                { 
                    Id = x.Id, 
                    LiveSessionId = x.LiveSessionId, 
                    ConnectedAt = x.ConnectedAt, 
                    UserId = x.UserId, 
                    AnonymousIdentifier = x.AnonymousIdentifier 
                }).ToList(), cancellationToken);
        }

        string ToListenerKey(Guid id, Guid? userId, string? anonymousIdentifier)
        {
            if (userId.HasValue) return $"u:{userId.Value}";
            if (!string.IsNullOrWhiteSpace(anonymousIdentifier)) return $"a:{anonymousIdentifier}";
            return $"g:{id}";
        }

        var uniqueListeners = listenerRows
            .Select(x => ToListenerKey(x.Id, x.UserId, x.AnonymousIdentifier))
            .Distinct()
            .Count();

        var peakListeners = hostSessionIds.Any() && listenerRows.Any() 
            ? hostSessionIds.Max(sId => listenerRows
                .Where(x => x.LiveSessionId == sId)
                .Select(x => ToListenerKey(x.Id, x.UserId, x.AnonymousIdentifier))
                .Distinct()
                .Count()) 
            : 0;

        // Fetch Song requests
        var songRequests = new List<SongRequest>();
        if (hostSessionIds.Any())
        {
            songRequests = await _context.SongRequests
                .Include(x => x.MediaFile)
                .AsNoTracking()
                .Where(x => hostSessionIds.Contains(x.LiveSessionId) && x.RequestedAt >= fromDate)
                .ToListAsync(cancellationToken);
        }

        var totalMusicRequests = songRequests.Count;

        // Fetch Chat messages
        var chatMessages = new List<LiveSessionChat>();
        if (hostSessionIds.Any())
        {
            chatMessages = await _context.LiveSessionChats
                .AsNoTracking()
                .Where(x => hostSessionIds.Contains(x.LiveSessionId) && x.CreatedAt >= fromDate)
                .ToListAsync(cancellationToken);
        }

        // Daily Chart Data
        var chartData = Enumerable.Range(0, normalizedDays)
            .Select(offset => fromDate.AddDays(offset))
            .Select(day =>
            {
                var uniqueListenersThatDay = listenerRows
                    .Where(x => x.ConnectedAt.Date == day)
                    .Select(x => ToListenerKey(x.Id, x.UserId, x.AnonymousIdentifier))
                    .Distinct()
                    .Count();

                var requestsThatDay = songRequests.Count(x => x.RequestedAt.Date == day);
                var chatsThatDay = chatMessages.Count(x => x.CreatedAt.Date == day);

                return new DailyHostAnalyticsMetric
                {
                    Date = day,
                    ListenersCount = uniqueListenersThatDay,
                    RequestsCount = requestsThatDay,
                    ChatCount = chatsThatDay
                };
            })
            .ToList();

        // Top requested songs
        var topRequestedSongs = songRequests
            .GroupBy(x => x.MediaFileId)
            .Select(g => new
            {
                Count = g.Count(),
                Song = g.First().MediaFile
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .Select((x, index) => new TopSongRequestMetric
            {
                Rank = index + 1,
                Title = x.Song.Title ?? "Unknown Title",
                Artist = x.Song.Artist,
                Count = x.Count
            })
            .ToList();

        // Ended Sessions Analysis
        var endedSessions = hostSessions
            .Where(x => x.Status == Domain.Enums.SessionStatus.Ended)
            .OrderByDescending(x => x.EndedAt)
            .Take(10) // Limit to last 10 ended sessions for performance
            .ToList();

        var endedSessionIds = endedSessions.Select(x => x.Id).ToList();
        var musicRequestsBySession = new Dictionary<Guid, int>();
        
        if (endedSessionIds.Any())
        {
            musicRequestsBySession = songRequests
                .Where(x => endedSessionIds.Contains(x.LiveSessionId))
                .GroupBy(x => x.LiveSessionId)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        var endedSessionsAnalysis = endedSessions.Select(s => 
        {
            var durationSpan = s.EndedAt.HasValue && s.StartedAt.HasValue 
                ? s.EndedAt.Value - s.StartedAt.Value 
                : TimeSpan.Zero;
            
            var sessionUniqueListeners = listenerRows
                .Where(x => x.LiveSessionId == s.Id)
                .Select(x => ToListenerKey(x.Id, x.UserId, x.AnonymousIdentifier))
                .Distinct()
                .Count();

            return new EndedSessionAnalysis
            {
                SessionId = s.Id,
                SessionName = s.SessionName,
                EndedAt = s.EndedAt,
                TotalDurationMinutes = durationSpan.TotalMinutes,
                TotalListeners = sessionUniqueListeners,
                MusicRequestsCount = musicRequestsBySession.ContainsKey(s.Id) ? musicRequestsBySession[s.Id] : 0
            };
        }).ToList();

        return new HostAnalyticsOverview
        {
            TotalListeners = uniqueListeners,
            TotalSessions = totalSessions,
            PeakListeners = peakListeners,
            TotalMusicRequests = totalMusicRequests,
            ChartData = chartData,
            TopRequestedSongs = topRequestedSongs,
            EndedSessionsAnalysis = endedSessionsAnalysis
        };
    }

    public async Task<StaffAnalyticsOverview> GetStaffAnalyticsOverviewAsync(int days, CancellationToken cancellationToken = default)
    {
        var fromDate = DateTime.UtcNow.Date.AddDays(-days);

        // 1. System Music
        var systemMedia = await _context.MediaFiles
            .AsNoTracking()
            .Where(x => x.OriginalSourceType == "system")
            .Select(x => new { x.Id, x.FileSizeBytes, x.UploadedAt })
            .ToListAsync(cancellationToken);

        var totalSystemMusic = systemMedia.Count;
        var totalStorageBytes = systemMedia.Sum(x => x.FileSizeBytes);

        // 2. Stations
        var totalStations = await _context.AzuraCastStations
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // 3. Song Requests
        var songRequests = await _context.SongRequests
            .Include(x => x.MediaFile)
            .AsNoTracking()
            .Where(x => x.RequestedAt >= fromDate)
            .ToListAsync(cancellationToken);

        var pendingSongRequests = songRequests.Count(x => x.Status == LiveSessionService.Domain.Enums.SongRequestStatus.Pending);

        // 4. Charts
        var contentGrowthChart = Enumerable.Range(0, days)
            .Select(offset => fromDate.AddDays(offset))
            .Select(day =>
            {
                return new DailyContentGrowthMetric
                {
                    Date = day,
                    NewMusicCount = systemMedia.Count(x => x.UploadedAt.Date == day)
                };
            })
            .ToList();

        var moderationChart = Enumerable.Range(0, days)
            .Select(offset => fromDate.AddDays(offset))
            .Select(day =>
            {
                var requestsThatDay = songRequests.Where(x => x.RequestedAt.Date == day).ToList();
                return new DailyModerationMetric
                {
                    Date = day,
                    PendingCount = requestsThatDay.Count(x => x.Status == LiveSessionService.Domain.Enums.SongRequestStatus.Pending),
                    ResolvedCount = requestsThatDay.Count(x => x.Status != LiveSessionService.Domain.Enums.SongRequestStatus.Pending)
                };
            })
            .ToList();

        return new StaffAnalyticsOverview
        {
            TotalSystemMusic = totalSystemMusic,
            TotalStorageBytes = totalStorageBytes,
            TotalStations = totalStations,
            PendingSongRequests = pendingSongRequests,
            ContentGrowthChart = contentGrowthChart,
            ModerationChart = moderationChart
        };
    }

    public async Task<List<LiveSessionChat>> GetSessionChatsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.LiveSessionChats
            .AsNoTracking()
            .Where(x => x.LiveSessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
