using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.ReviewPodcastEpisodeRequest;

public sealed class ReviewPodcastEpisodeRequestHandler
    : ICommandHandler<ReviewPodcastEpisodeRequestCommand, PodcastEpisodeRequestResult>
{
    private readonly IPodcastEpisodeRequestRepository _repository;
    private readonly IPodcastRepository _podcastRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ReviewPodcastEpisodeRequestHandler> _logger;

    public ReviewPodcastEpisodeRequestHandler(
        IPodcastEpisodeRequestRepository repository,
        IPodcastRepository podcastRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ReviewPodcastEpisodeRequestHandler> logger)
    {
        _repository = repository;
        _podcastRepository = podcastRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<PodcastEpisodeRequestResult>> Handle(
        ReviewPodcastEpisodeRequestCommand command,
        CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(command.RequestId, cancellationToken);
        if (request == null)
            return Result<PodcastEpisodeRequestResult>.Failure("Episode request not found", ErrorCode.NotFound);

        if (request.Status != PodcastRequestStatus.Pending)
            return Result<PodcastEpisodeRequestResult>.Failure("Request has already been reviewed", ErrorCode.BadRequest);

        if (command.IsApproved)
        {
            var targetPodcast = await _podcastRepository.GetByIdAsync(request.PodcastId, cancellationToken);
            if (targetPodcast == null)
            {
                request.Status = PodcastRequestStatus.Rejected;
                request.RejectReason = "Target podcast no longer exists";
                _logger.LogWarning("Episode request {Id} approved but target podcast {PodcastId} missing", request.Id, request.PodcastId);
            }
            else
            {
                var existingEpisodes = await _podcastRepository.GetEpisodesByPodcastIdAsync(request.PodcastId, cancellationToken);
                var nextEpisodeNumber = existingEpisodes.Count > 0 ? existingEpisodes.Max(e => e.EpisodeNumber) + 1 : 1;

                var newEpisode = new PodcastEpisode
                {
                    Id = Guid.NewGuid(),
                    PodcastId = request.PodcastId,
                    Title = request.Title,
                    Description = request.Description,
                    ThumbnailUrl = request.ThumbnailUrl,
                    AudioUrl = request.AudioUrl,
                    Duration = request.Duration,
                    EpisodeNumber = nextEpisodeNumber,
                    PublishDate = _dateTimeProvider.UtcNow
                };

                await _podcastRepository.AddEpisodeAsync(newEpisode, cancellationToken);
                request.Status = PodcastRequestStatus.Approved;
                _logger.LogInformation("Episode request {Id} approved. Episode {EpisodeId} added to Podcast {PodcastId}", request.Id, newEpisode.Id, request.PodcastId);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.RejectReason))
                return Result<PodcastEpisodeRequestResult>.Failure("Reject reason is required", ErrorCode.BadRequest);

            request.Status = PodcastRequestStatus.Rejected;
            request.RejectReason = command.RejectReason;
            _logger.LogInformation("Episode request {Id} rejected: {Reason}", request.Id, command.RejectReason);
        }

        request.ReviewedByUserId = command.ReviewedByUserId;
        request.ReviewedAt = _dateTimeProvider.UtcNow;

        await _repository.UpdateAsync(request, cancellationToken);

        return Result<PodcastEpisodeRequestResult>.Success(new PodcastEpisodeRequestResult
        {
            Id = request.Id,
            PodcastId = request.PodcastId,
            RequestedByUserId = request.RequestedByUserId,
            AuthorInfo = string.IsNullOrWhiteSpace(request.AuthorInfo) ? null : System.Text.Json.JsonSerializer.Deserialize<object>(request.AuthorInfo),
            Title = request.Title,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            AudioUrl = request.AudioUrl,
            Duration = request.Duration,
            Status = request.Status.ToString(),
            ReviewedByUserId = request.ReviewedByUserId,
            ReviewedAt = request.ReviewedAt,
            RejectReason = request.RejectReason,
            RequestedAt = request.RequestedAt
        });
    }
}
