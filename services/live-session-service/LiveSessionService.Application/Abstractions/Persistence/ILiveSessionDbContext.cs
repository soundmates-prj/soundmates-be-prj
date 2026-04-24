using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Application.Abstractions.Persistence;

/// <summary>
/// Abstraction over LiveSessionDbContext to allow Application layer to query
/// the database without depending on Infrastructure directly.
/// </summary>
public interface ILiveSessionDbContext
{
    DbSet<UserPurchasedPodcast> UserPurchasedPodcasts { get; }
    DbSet<UserSavedPodcast> UserSavedPodcasts { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
