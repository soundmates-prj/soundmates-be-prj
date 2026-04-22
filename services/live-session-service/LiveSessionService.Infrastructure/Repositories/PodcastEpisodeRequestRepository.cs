using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public class PodcastEpisodeRequestRepository : IPodcastEpisodeRequestRepository
{
    private readonly LiveSessionDbContext _dbContext;

    public PodcastEpisodeRequestRepository(LiveSessionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PodcastEpisodeRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PodcastEpisodeRequests.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<List<PodcastEpisodeRequest>> GetAllAsync(PodcastRequestStatus? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PodcastEpisodeRequests.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLowerInvariant();
            query = query.Where(r => r.Title.ToLower().Contains(search));
        }

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PodcastEpisodeRequest>> GetByUserIdAsync(Guid userId, PodcastRequestStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PodcastEpisodeRequests.Where(r => r.RequestedByUserId == userId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PodcastEpisodeRequest> AddAsync(PodcastEpisodeRequest request, CancellationToken cancellationToken = default)
    {
        await _dbContext.PodcastEpisodeRequests.AddAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task UpdateAsync(PodcastEpisodeRequest request, CancellationToken cancellationToken = default)
    {
        _dbContext.PodcastEpisodeRequests.Update(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
