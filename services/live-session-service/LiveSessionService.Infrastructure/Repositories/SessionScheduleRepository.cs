using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class SessionScheduleRepository : ISessionScheduleRepository
{
    private readonly LiveSessionDbContext _context;

    public SessionScheduleRepository(LiveSessionDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SessionSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _context.SessionSchedules.AddAsync(schedule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SessionSchedule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SessionSchedules
            .AsNoTracking()
            .Include(x => x.LiveSession)
                .ThenInclude(x => x.AzuraCastStation)
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<SessionSchedule?> GetByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        return await _context.SessionSchedules
            .FirstOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);
    }

    public async Task<SessionSchedule?> GetByIdWithSessionAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        return await _context.SessionSchedules
            .AsNoTracking()
            .Include(x => x.LiveSession)
                .ThenInclude(x => x.AzuraCastStation)
            .FirstOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);
    }

    public async Task<List<SessionSchedule>> GetByLiveSessionIdAsync(Guid liveSessionId, CancellationToken cancellationToken = default)
    {
        return await _context.SessionSchedules
            .Where(x => x.LiveSessionId == liveSessionId)
            .Include(x => x.LiveSession)
                .ThenInclude(x => x.AzuraCastStation)
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(SessionSchedule schedule, CancellationToken cancellationToken = default)
    {
        _context.SessionSchedules.Update(schedule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(SessionSchedule schedule, CancellationToken cancellationToken = default)
    {
        _context.SessionSchedules.Remove(schedule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SessionSchedule?> GetLatestByLiveSessionIdAsync(Guid liveSessionId, CancellationToken cancellationToken = default)
    {
        return await _context.SessionSchedules
            .Where(x => x.LiveSessionId == liveSessionId)
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, SessionSchedule>> GetLatestByLiveSessionIdsAsync(
        IReadOnlyCollection<Guid> liveSessionIds,
        CancellationToken cancellationToken = default)
    {
        if (liveSessionIds.Count == 0)
        {
            return new Dictionary<Guid, SessionSchedule>();
        }

        var schedules = await _context.SessionSchedules
            .Where(x => liveSessionIds.Contains(x.LiveSessionId))
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return schedules
            .GroupBy(x => x.LiveSessionId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<Dictionary<Guid, List<SessionSchedule>>> GetByLiveSessionIdsAsync(
        IReadOnlyCollection<Guid> liveSessionIds,
        CancellationToken cancellationToken = default)
    {
        if (liveSessionIds.Count == 0)
        {
            return new Dictionary<Guid, List<SessionSchedule>>();
        }

        var schedules = await _context.SessionSchedules
            .Where(x => liveSessionIds.Contains(x.LiveSessionId))
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return schedules
            .GroupBy(x => x.LiveSessionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<(List<SessionSchedule> Items, int TotalCount)> SearchAsync(
        string? keyword,
        Domain.Enums.ScheduleStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SessionSchedules
            .AsNoTracking()
            .Include(x => x.LiveSession)
                .ThenInclude(x => x.AzuraCastStation)
            .AsQueryable();

        // Keyword filter on title and session name
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(x =>
                (x.Title != null && EF.Functions.ILike(x.Title, $"%{kw}%")) ||
                (x.LiveSession.SessionName != null && EF.Functions.ILike(x.LiveSession.SessionName, $"%{kw}%")));
        }

        // Status filter
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        // Date range filter
        if (fromDate.HasValue)
        {
            query = query.Where(x => x.StartDate >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(x => x.StartDate <= toDate.Value);
        }

        // Total count (before pagination)
        var totalCount = await query.CountAsync(cancellationToken);

        // Paginated results
        var items = await query
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
