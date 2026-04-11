using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class PodcastRequestRepository : IPodcastRequestRepository
{
    private readonly LiveSessionDbContext _dbContext;

    public PodcastRequestRepository(LiveSessionDbContext dbContext) => _dbContext = dbContext;

    public async Task<PodcastRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.PodcastRequests
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<PodcastRequest?> GetByIdWithSessionAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.PodcastRequests
            .Include(r => r.LiveSession)
            .ThenInclude(s => s.AzuraCastStation)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<PodcastRequest>> GetByLiveSessionIdAsync(
        Guid liveSessionId,
        PodcastRequestStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.PodcastRequests
            .Where(r => r.LiveSessionId == liveSessionId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PodcastRequest>> GetAllAsync(
        Guid? liveSessionId = null,
        PodcastRequestStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.PodcastRequests.AsQueryable();

        if (liveSessionId.HasValue)
            query = query.Where(r => r.LiveSessionId == liveSessionId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);
    }

    public Task AddAsync(PodcastRequest request, CancellationToken ct = default)
    {
        _dbContext.PodcastRequests.Add(request);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PodcastRequest request, CancellationToken ct = default)
    {
        _dbContext.PodcastRequests.Update(request);
        return Task.CompletedTask;
    }
}