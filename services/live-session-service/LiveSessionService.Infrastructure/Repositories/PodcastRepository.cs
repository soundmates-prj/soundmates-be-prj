using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class PodcastRepository : IPodcastRepository
{
    private readonly LiveSessionDbContext _context;

    public PodcastRepository(LiveSessionDbContext context)
    {
        _context = context;
    }

    public Task<Podcast?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Podcasts
            .Include(x => x.Episodes)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Podcast>> GetAllAsync(
        Guid? createdBy = null,
        PodcastStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Podcast> query = _context.Podcasts
            .AsNoTracking()
            .Include(x => x.Episodes);

        if (createdBy.HasValue)
        {
            query = query.Where(x => x.CreatedBy == createdBy.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Podcast podcast, CancellationToken cancellationToken = default)
    {
        await _context.Podcasts.AddAsync(podcast, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Podcast podcast, CancellationToken cancellationToken = default)
    {
        _context.Podcasts.Update(podcast);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Podcast podcast, CancellationToken cancellationToken = default)
    {
        _context.Podcasts.Remove(podcast);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
