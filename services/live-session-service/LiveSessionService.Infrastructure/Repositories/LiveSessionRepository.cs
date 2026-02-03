using Microsoft.EntityFrameworkCore;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
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

    public async Task<LiveSession?> GetByIdWithStationAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.LiveSessions
            .Include(x => x.AzuraCastStation)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<LiveSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LiveSessions
            .Include(x => x.AzuraCastStation)
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
}
