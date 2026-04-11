using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;

public sealed class CreatePodcastRequestHandler
    : ICommandHandler<CreatePodcastRequestCommand, PodcastRequestResult>
{
    private readonly IPodcastRequestRepository _repository;
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreatePodcastRequestHandler> _logger;

    public CreatePodcastRequestHandler(
        IPodcastRequestRepository repository,
        ILiveSessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreatePodcastRequestHandler> logger)
    {
        _repository = repository;
        _sessionRepository = sessionRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<PodcastRequestResult>> Handle(
        CreatePodcastRequestCommand command,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithStationAsync(command.LiveSessionId, cancellationToken);
        if (session == null)
        {
            return Result<PodcastRequestResult>.Failure(
                "Live session not found",
                ErrorCode.NotFound);
        }

        if (session.Status != SessionStatus.Live && session.Status != SessionStatus.Paused)
        {
            return Result<PodcastRequestResult>.Failure(
                "Live session is not active",
                ErrorCode.BadRequest);
        }

        var now = _dateTimeProvider.UtcNow;

        var podcastRequest = new Domain.Entities.PodcastRequest
        {
            Id = Guid.NewGuid(),
            LiveSessionId = command.LiveSessionId,
            RequestedByUserId = command.RequestedByUserId,
            Title = command.Title.Trim(),
            Description = command.Description?.Trim(),
            ScriptText = command.ScriptText,
            AudioUrl = command.AudioUrl.Trim(),
            DurationSeconds = command.DurationSeconds,
            VoiceCode = command.VoiceCode.Trim(),
            VoiceDisplayName = command.VoiceDisplayName?.Trim(),
            Status = PodcastRequestStatus.Pending,
            RequestedAt = now
        };

        await _repository.AddAsync(podcastRequest, cancellationToken);

        _logger.LogInformation(
            "PodcastRequest {Id} created for session {SessionId} by user {UserId}",
            podcastRequest.Id, command.LiveSessionId, command.RequestedByUserId);

        return Result<PodcastRequestResult>.Success(new PodcastRequestResult
        {
            Id = podcastRequest.Id,
            LiveSessionId = podcastRequest.LiveSessionId,
            RequestedByUserId = podcastRequest.RequestedByUserId,
            Title = podcastRequest.Title,
            Description = podcastRequest.Description,
            ScriptText = podcastRequest.ScriptText,
            AudioUrl = podcastRequest.AudioUrl,
            DurationSeconds = podcastRequest.DurationSeconds,
            VoiceCode = podcastRequest.VoiceCode,
            VoiceDisplayName = podcastRequest.VoiceDisplayName,
            AzuraCastMediaId = podcastRequest.AzuraCastMediaId,
            Status = podcastRequest.Status.ToString(),
            ReviewedByUserId = podcastRequest.ReviewedByUserId,
            ReviewedAt = podcastRequest.ReviewedAt,
            RejectReason = podcastRequest.RejectReason,
            RequestedAt = podcastRequest.RequestedAt,
            SessionName = session.SessionName
        });
    }
}