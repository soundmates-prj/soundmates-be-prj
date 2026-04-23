using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.PodcastRequests.Commands.ReviewPodcastRequest;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.ReviewPodcastRequest;

public sealed class ReviewPodcastRequestHandler
    : ICommandHandler<ReviewPodcastRequestCommand, PodcastRequestResult>
{
    private readonly IPodcastRequestRepository _repository;
    private readonly IPodcastRepository _podcastRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ReviewPodcastRequestHandler> _logger;

    public ReviewPodcastRequestHandler(
        IPodcastRequestRepository repository,
        IPodcastRepository podcastRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ReviewPodcastRequestHandler> logger)
    {
        _repository = repository;
        _podcastRepository = podcastRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<PodcastRequestResult>> Handle(
        ReviewPodcastRequestCommand command,
        CancellationToken cancellationToken)
    {
        var podcastRequest = await _repository.GetByIdAsync(command.PodcastRequestId, cancellationToken);
        if (podcastRequest == null)
        {
            return Result<PodcastRequestResult>.Failure(
                "Podcast request not found",
                ErrorCode.NotFound);
        }

        if (podcastRequest.Status != PodcastRequestStatus.Pending)
        {
            return Result<PodcastRequestResult>.Failure(
                "Podcast request has already been reviewed",
                ErrorCode.BadRequest);
        }

        if (command.IsApproved)
        {
            _logger.LogInformation(
                "Approving podcast request {Id}, creating new Podcast and Episode records.",
                podcastRequest.Id);

            var now = _dateTimeProvider.UtcNow;

            var newPodcast = new Podcast
            {
                Id = Guid.NewGuid(),
                Title = podcastRequest.Title,
                Description = podcastRequest.Description,
                Author = podcastRequest.AuthorInfo,
                Status = PodcastStatus.Published,
                Banner = podcastRequest.BannerUrl,
                Price = podcastRequest.Price,
                IsPaid = podcastRequest.IsPaid,
                CreatedAt = now,
                CreatedBy = podcastRequest.RequestedByUserId
            };

            await _podcastRepository.AddAsync(newPodcast, cancellationToken);

            podcastRequest.Status = PodcastRequestStatus.Approved;

            _logger.LogInformation(
                "Podcast request {Id} approved. Podcast created: {PodcastId}",
                podcastRequest.Id, newPodcast.Id);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.RejectReason))
            {
                return Result<PodcastRequestResult>.Failure(
                    "Reject reason is required",
                    ErrorCode.BadRequest);
            }

            podcastRequest.Status = PodcastRequestStatus.Rejected;
            podcastRequest.RejectReason = command.RejectReason;

            _logger.LogInformation(
                "Podcast request {Id} rejected: {Reason}",
                podcastRequest.Id, command.RejectReason);
        }

        podcastRequest.ReviewedByUserId = command.ReviewedByUserId;
        podcastRequest.ReviewedAt = _dateTimeProvider.UtcNow;

        await _repository.UpdateAsync(podcastRequest, cancellationToken);

        return Result<PodcastRequestResult>.Success(new PodcastRequestResult
        {
            Id = podcastRequest.Id,
            RequestedByUserId = podcastRequest.RequestedByUserId,
            AuthorInfo = string.IsNullOrWhiteSpace(podcastRequest.AuthorInfo) ? null : System.Text.Json.JsonSerializer.Deserialize<object>(podcastRequest.AuthorInfo),
            Title = podcastRequest.Title,
            Type = podcastRequest.Type,
            Description = podcastRequest.Description,
            BannerUrl = podcastRequest.BannerUrl,
            Price = podcastRequest.Price,
            IsPaid = podcastRequest.IsPaid,
            Status = podcastRequest.Status.ToString(),
            ReviewedByUserId = podcastRequest.ReviewedByUserId,
            ReviewedAt = podcastRequest.ReviewedAt,
            RejectReason = podcastRequest.RejectReason,
            RequestedAt = podcastRequest.RequestedAt
        });
    }
}