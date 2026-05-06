using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using shared.Contracts.Events.Notifications;
using System.Text.Json;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.CreatePodcastEpisodeRequest;

public sealed class CreatePodcastEpisodeRequestHandler
    : ICommandHandler<CreatePodcastEpisodeRequestCommand, PodcastEpisodeRequestResult>
{
    private readonly IPodcastEpisodeRequestRepository _repository;
    private readonly IPodcastRepository _podcastRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreatePodcastEpisodeRequestHandler> _logger;
    private readonly IMessageBusPublisher _eventBus;

    public CreatePodcastEpisodeRequestHandler(
        IPodcastEpisodeRequestRepository repository,
        IPodcastRepository podcastRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreatePodcastEpisodeRequestHandler> logger,
        IMessageBusPublisher eventBus)
    {
        _repository = repository;
        _podcastRepository = podcastRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<Result<PodcastEpisodeRequestResult>> Handle(
        CreatePodcastEpisodeRequestCommand command,
        CancellationToken cancellationToken)
    {
        var targetPodcast = await _podcastRepository.GetByIdAsync(command.PodcastId, cancellationToken);
        if (targetPodcast == null)
            return Result<PodcastEpisodeRequestResult>.Failure("Target podcast does not exist", ErrorCode.BadRequest);

        var now = _dateTimeProvider.UtcNow;
        var request = new PodcastEpisodeRequest
        {
            Id = Guid.NewGuid(),
            PodcastId = command.PodcastId,
            RequestedByUserId = command.RequestedByUserId,
            AuthorInfo = command.AuthorInfo,
            Title = command.Title.Trim(),
            Description = command.Description?.Trim(),
            ThumbnailUrl = command.ThumbnailUrl?.Trim(),
            AudioUrl = command.AudioUrl.Trim(),
            Duration = command.Duration,
            Status = PodcastRequestStatus.Pending,
            RequestedAt = now
        };

        await _repository.AddAsync(request, cancellationToken);

        _logger.LogInformation("PodcastEpisodeRequest {Id} created for Podcast {PodcastId} by user {UserId}",
            request.Id, request.PodcastId, command.RequestedByUserId);

        var notificationEvent = new NotificationEvent
        {
            Title = "Yêu cầu đăng tập Podcast mới",
            Message = $"Có yêu cầu đăng tập '{request.Title}' đang chờ duyệt.",
            TargetRole = "ADMIN",
            ReferenceId = request.Id,
            Type = "podcast_episode_request_created"
        };
        await _eventBus.PublishAsync("notification.created", JsonSerializer.Serialize(notificationEvent), cancellationToken);

        return Result<PodcastEpisodeRequestResult>.Success(new PodcastEpisodeRequestResult
        {
            Id = request.Id,
            PodcastId = request.PodcastId,
            RequestedByUserId = request.RequestedByUserId,
            AuthorInfo = parsedAuthorInfo,
            Title = request.Title,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            AudioUrl = request.AudioUrl,
            Duration = request.Duration,
            Status = request.Status.ToString(),
            RequestedAt = request.RequestedAt
        });
    }
}
