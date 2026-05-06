using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using shared.Contracts.Events.Notifications;
using System.Text.Json;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;

public sealed class CreatePodcastRequestHandler
    : ICommandHandler<CreatePodcastRequestCommand, PodcastRequestResult>
{
    private readonly IPodcastRequestRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreatePodcastRequestHandler> _logger;
    private readonly IMessageBusPublisher _eventBus;

    public CreatePodcastRequestHandler(
        IPodcastRequestRepository repository,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreatePodcastRequestHandler> logger,
        IMessageBusPublisher eventBus)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<Result<PodcastRequestResult>> Handle(
        CreatePodcastRequestCommand command,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var podcastRequest = new Domain.Entities.PodcastRequest
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = command.RequestedByUserId,
            TargetPodcastId = command.TargetPodcastId,
            AuthorInfo = command.AuthorInfo,
            Title = command.Title.Trim(),
            Type = command.Type.Trim(),
            Description = command.Description?.Trim(),
            BannerUrl = command.BannerUrl?.Trim(),
            Price = command.Price,
            IsPaid = command.IsPaid,
            Status = PodcastRequestStatus.Pending,
            RequestedAt = now
        };

        await _repository.AddAsync(podcastRequest, cancellationToken);

        _logger.LogInformation(
            "PodcastRequest {Id} created for series '{Title}' by user {UserId}",
            podcastRequest.Id, podcastRequest.Title, command.RequestedByUserId);

        var notificationEvent = new NotificationEvent
        {
            Title = "Yêu cầu tạo Podcast mới",
            Message = $"Có yêu cầu tạo podcast '{podcastRequest.Title}' đang chờ duyệt.",
            TargetRole = "ADMIN",
            ReferenceId = podcastRequest.Id,
            Type = "podcast_request_created"
        };
        await _eventBus.PublishAsync("notification.created", JsonSerializer.Serialize(notificationEvent), cancellationToken);

        return Result<PodcastRequestResult>.Success(new PodcastRequestResult
        {
            Id = podcastRequest.Id,
            RequestedByUserId = podcastRequest.RequestedByUserId,
            TargetPodcastId = podcastRequest.TargetPodcastId,
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