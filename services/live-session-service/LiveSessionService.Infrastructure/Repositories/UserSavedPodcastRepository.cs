using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class UserSavedPodcastRepository : IUserSavedPodcastRepository
{
    private readonly LiveSessionDbContext _context;

    public UserSavedPodcastRepository(LiveSessionDbContext context)
    {
        _context = context;
    }

    public Task<UserSavedPodcast?> GetByUserAndPodcastAsync(Guid userId, Guid podcastId, CancellationToken cancellationToken = default)
        => _context.UserSavedPodcasts
            .Include(x => x.Podcast)
                .ThenInclude(x => x.Episodes)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.PodcastId == podcastId, cancellationToken);

    public Task<List<UserSavedPodcast>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.UserSavedPodcasts
            .AsNoTracking()
            .Include(x => x.Podcast)
                .ThenInclude(x => x.Episodes)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SavedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserSavedPodcast userSavedPodcast, CancellationToken cancellationToken = default)
    {
        await _context.UserSavedPodcasts.AddAsync(userSavedPodcast, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserSavedPodcast userSavedPodcast, CancellationToken cancellationToken = default)
    {
        _context.UserSavedPodcasts.Remove(userSavedPodcast);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
