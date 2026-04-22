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

    public async Task<IReadOnlyList<PodcastRequest>> GetAllAsync(
        PodcastRequestStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.PodcastRequests.AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(PodcastRequest request, CancellationToken ct = default)
    {
        await _dbContext.PodcastRequests.AddAsync(request, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PodcastRequest request, CancellationToken ct = default)
    {
        _dbContext.PodcastRequests.Update(request);
        await _dbContext.SaveChangesAsync(ct);
    }
}