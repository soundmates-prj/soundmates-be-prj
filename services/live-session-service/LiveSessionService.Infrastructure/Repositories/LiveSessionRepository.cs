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

        var totalSessionsTask = _context.LiveSessions.CountAsync(cancellationToken);
        var liveSessionsTask = _context.LiveSessions.CountAsync(
            x => x.Status == Domain.Enums.SessionStatus.Live,
            cancellationToken);

        var listenerRows = await _context.SessionListeners
            .AsNoTracking()
            .Where(x => x.ConnectedAt >= fromDate)
            .Select(x => new { x.Id, x.ConnectedAt, x.UserId, x.AnonymousIdentifier })
            .ToListAsync(cancellationToken);

        await Task.WhenAll(totalSessionsTask, liveSessionsTask);

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
            TotalSessions = totalSessionsTask.Result,
            LiveSessions = liveSessionsTask.Result,
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
}
