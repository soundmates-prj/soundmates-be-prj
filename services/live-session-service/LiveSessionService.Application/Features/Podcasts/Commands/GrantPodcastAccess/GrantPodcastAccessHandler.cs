using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Abstractions.Persistence;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LiveSessionService.Application.Features.Podcasts.Commands.GrantPodcastAccess;

public sealed class GrantPodcastAccessHandler : ICommandHandler<GrantPodcastAccessCommand>
{
    private readonly ILiveSessionDbContext _dbContext;
    private readonly IPodcastRepository _podcastRepository;

    public GrantPodcastAccessHandler(ILiveSessionDbContext dbContext, IPodcastRepository podcastRepository)
    {
        _dbContext = dbContext;
        _podcastRepository = podcastRepository;
    }

    public async Task<Result> Handle(GrantPodcastAccessCommand request, CancellationToken cancellationToken)
    {
        var podcast = await _podcastRepository.GetByIdAsync(request.PodcastId, cancellationToken);
        if (podcast == null)
            return Result.Failure("Podcast not found", ErrorCode.NotFound);

        // Check if already purchased
        var existingPurchase = await _dbContext.UserPurchasedPodcasts
            .FirstOrDefaultAsync(x => x.PodcastId == request.PodcastId && x.UserId == request.UserId, cancellationToken);

        if (existingPurchase == null)
        {
            var purchasedPodcast = new UserPurchasedPodcast
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                PodcastId = request.PodcastId,
                Price = request.Price,
                PurchasedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.UserPurchasedPodcasts.AddAsync(purchasedPodcast, cancellationToken);
        }

        // Add to Saved Podcasts if not already saved
        var existingSaved = await _dbContext.UserSavedPodcasts
            .FirstOrDefaultAsync(x => x.PodcastId == request.PodcastId && x.UserId == request.UserId, cancellationToken);

        if (existingSaved == null)
        {
            var savedPodcast = new UserSavedPodcast
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                PodcastId = request.PodcastId,
                SavedAt = DateTime.UtcNow
            };
            
            await _dbContext.UserSavedPodcasts.AddAsync(savedPodcast, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
