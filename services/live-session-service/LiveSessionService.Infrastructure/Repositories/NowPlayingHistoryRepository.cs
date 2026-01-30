using Microsoft.EntityFrameworkCore;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class NowPlayingHistoryRepository : INowPlayingHistoryRepository
{
    private readonly LiveSessionDbContext _context;

    public NowPlayingHistoryRepository(LiveSessionDbContext context)
    {
        _context = context;
    }

    public async Task<NowPlayingHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.NowPlayingHistory
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<NowPlayingHistory?> GetLatestBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.NowPlayingHistory
            .Where(x => x.LiveSessionId == sessionId)
            .OrderByDescending(x => x.PlayedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<NowPlayingHistory>> GetBySessionIdAsync(
        Guid sessionId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.NowPlayingHistory
            .Where(x => x.LiveSessionId == sessionId)
            .OrderByDescending(x => x.PlayedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NowPlayingHistory history, CancellationToken cancellationToken = default)
    {
        await _context.NowPlayingHistory.AddAsync(history, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(NowPlayingHistory history, CancellationToken cancellationToken = default)
    {
        _context.NowPlayingHistory.Update(history);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
