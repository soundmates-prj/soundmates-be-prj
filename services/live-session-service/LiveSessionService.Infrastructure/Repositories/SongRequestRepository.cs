using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class SongRequestRepository : ISongRequestRepository
{
    private readonly LiveSessionDbContext _context;

    public SongRequestRepository(LiveSessionDbContext context)
    {
        _context = context;
    }

    public async Task<SongRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SongRequests
            .Include(x => x.MediaFile)
            .Include(x => x.LiveSession)
                .ThenInclude(x => x.AzuraCastStation)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SongRequest>> GetByLiveSessionIdAsync(
        Guid liveSessionId,
        SongRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SongRequest> query = _context.SongRequests
            .AsNoTracking()
            .Include(x => x.MediaFile)
            .Where(x => x.LiveSessionId == liveSessionId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SongRequest songRequest, CancellationToken cancellationToken = default)
    {
        await _context.SongRequests.AddAsync(songRequest, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SongRequest songRequest, CancellationToken cancellationToken = default)
    {
        _context.SongRequests.Update(songRequest);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountRequestsByUserTodayAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.SongRequests
            .Where(x => x.RequestedByUserId == userId && x.RequestedAt >= today)
            .CountAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<SongRequest> Items, int TotalCount)> GetAllAsync(
        SongRequestStatus? status = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SongRequest> query = _context.SongRequests
            .AsNoTracking()
            .Include(x => x.MediaFile);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
