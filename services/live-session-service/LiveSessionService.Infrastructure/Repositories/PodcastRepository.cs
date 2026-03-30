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

    public Task<PodcastEpisode?> GetEpisodeByIdAsync(Guid episodeId, CancellationToken cancellationToken = default)
        => _context.PodcastEpisodes
            .FirstOrDefaultAsync(x => x.Id == episodeId, cancellationToken);

    public async Task<IReadOnlyList<PodcastEpisode>> GetEpisodesByPodcastIdAsync(Guid podcastId, CancellationToken cancellationToken = default)
        => await _context.PodcastEpisodes
            .Where(x => x.PodcastId == podcastId)
            .OrderBy(x => x.EpisodeNumber)
            .ThenByDescending(x => x.PublishDate)
            .ToListAsync(cancellationToken);

    public Task<PodcastEpisode?> GetEpisodeByNumberAsync(Guid podcastId, int episodeNumber, CancellationToken cancellationToken = default)
        => _context.PodcastEpisodes
            .FirstOrDefaultAsync(x => x.PodcastId == podcastId && x.EpisodeNumber == episodeNumber, cancellationToken);

    public async Task AddEpisodeAsync(PodcastEpisode episode, CancellationToken cancellationToken = default)
    {
        await _context.PodcastEpisodes.AddAsync(episode, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateEpisodeAsync(PodcastEpisode episode, CancellationToken cancellationToken = default)
    {
        _context.PodcastEpisodes.Update(episode);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteEpisodeAsync(PodcastEpisode episode, CancellationToken cancellationToken = default)
    {
        _context.PodcastEpisodes.Remove(episode);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
