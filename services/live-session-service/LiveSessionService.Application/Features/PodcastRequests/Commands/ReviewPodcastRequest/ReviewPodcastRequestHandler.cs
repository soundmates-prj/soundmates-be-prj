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
using shared.Contracts.Events.Notifications;
using System.Text.Json;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.ReviewPodcastRequest;

public sealed class ReviewPodcastRequestHandler
    : ICommandHandler<ReviewPodcastRequestCommand, PodcastRequestResult>
{
    private readonly IPodcastRequestRepository _repository;
    private readonly IPodcastRepository _podcastRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ReviewPodcastRequestHandler> _logger;
    private readonly IMessageBusPublisher _eventBus;

    public ReviewPodcastRequestHandler(
        IPodcastRequestRepository repository,
        IPodcastRepository podcastRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ReviewPodcastRequestHandler> logger,
        IMessageBusPublisher eventBus)
    {
        _repository = repository;
        _podcastRepository = podcastRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _eventBus = eventBus;
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

            if (podcastRequest.TargetPodcastId.HasValue)
            {
                var existingPodcast = await _podcastRepository.GetByIdAsync(podcastRequest.TargetPodcastId.Value, cancellationToken);
                if (existingPodcast != null)
                {
                    existingPodcast.Title = podcastRequest.Title;
                    existingPodcast.Description = podcastRequest.Description;
                    existingPodcast.Banner = podcastRequest.BannerUrl ?? existingPodcast.Banner;
                    if (!string.IsNullOrWhiteSpace(podcastRequest.Type))
                    {
                        existingPodcast.Type = podcastRequest.Type;
                    }
                    existingPodcast.Price = podcastRequest.Price;
                    existingPodcast.IsPaid = podcastRequest.IsPaid;
                    existingPodcast.UpdatedAt = now;

                    await _podcastRepository.UpdateAsync(existingPodcast, cancellationToken);
                    _logger.LogInformation("Podcast request {Id} approved. Podcast updated: {PodcastId}", podcastRequest.Id, existingPodcast.Id);
                }
                else
                {
                    _logger.LogWarning("Podcast request {Id} approved but target podcast {TargetId} not found.", podcastRequest.Id, podcastRequest.TargetPodcastId.Value);
                }
            }
            else
            {
                var newPodcast = new Podcast
                {
                    Id = Guid.NewGuid(),
                    Title = podcastRequest.Title,
                    Description = podcastRequest.Description,
                    Author = podcastRequest.AuthorInfo,
                    Type = podcastRequest.Type,
                    Status = PodcastStatus.Published,
                    Banner = podcastRequest.BannerUrl,
                    Price = podcastRequest.Price,
                    IsPaid = podcastRequest.IsPaid,
                    CreatedAt = now,
                    CreatedBy = podcastRequest.RequestedByUserId
                };

                await _podcastRepository.AddAsync(newPodcast, cancellationToken);
                _logger.LogInformation("Podcast request {Id} approved. Podcast created: {PodcastId}", podcastRequest.Id, newPodcast.Id);
            }

            podcastRequest.Status = PodcastRequestStatus.Approved;
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

        var notificationEvent = new NotificationEvent
        {
            Title = command.IsApproved ? "Podcast đã được duyệt" : "Podcast bị từ chối",
            Message = command.IsApproved
                ? $"Yêu cầu tạo podcast '{podcastRequest.Title}' của bạn đã được phê duyệt."
                : $"Yêu cầu tạo podcast '{podcastRequest.Title}' của bạn đã bị từ chối. Lý do: {command.RejectReason}",
            ReceiveUserId = podcastRequest.RequestedByUserId,
            ReferenceId = podcastRequest.Id,
            Type = command.IsApproved ? "podcast_request_approved" : "podcast_request_rejected"
        };
        await _eventBus.PublishAsync("notification.created", JsonSerializer.Serialize(notificationEvent), cancellationToken);

        return Result<PodcastRequestResult>.Success(new PodcastRequestResult
        {
            Id = podcastRequest.Id,
            RequestedByUserId = podcastRequest.RequestedByUserId,
            AuthorInfo = parsedAuthorInfo,
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