using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.PodcastRequests.Commands.ReviewPodcastRequest;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.ReviewPodcastRequest;

public sealed class ReviewPodcastRequestHandler
    : ICommandHandler<ReviewPodcastRequestCommand, PodcastRequestResult>
{
    private readonly IPodcastRequestRepository _repository;
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IAzuraCastPodcastService _azuraCastPodcastService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ReviewPodcastRequestHandler> _logger;

    public ReviewPodcastRequestHandler(
        IPodcastRequestRepository repository,
        ILiveSessionRepository sessionRepository,
        IAzuraCastPodcastService azuraCastPodcastService,
        IDateTimeProvider dateTimeProvider,
        ILogger<ReviewPodcastRequestHandler> logger)
    {
        _repository = repository;
        _sessionRepository = sessionRepository;
        _azuraCastPodcastService = azuraCastPodcastService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<PodcastRequestResult>> Handle(
        ReviewPodcastRequestCommand command,
        CancellationToken cancellationToken)
    {
        var podcastRequest = await _repository.GetByIdWithSessionAsync(command.PodcastRequestId, cancellationToken);
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
            // Validate session has station
            var session = podcastRequest.LiveSession;
            if (session == null || session.AzuraCastStationId == null)
            {
                return Result<PodcastRequestResult>.Failure(
                    "Live session is not linked to a station",
                    ErrorCode.BadRequest);
            }

            var station = session.AzuraCastStation;
            if (station == null)
            {
                return Result<PodcastRequestResult>.Failure(
                    "AzuraCast station not found",
                    ErrorCode.BadRequest);
            }

            _logger.LogInformation(
                "Approving podcast request {Id}, uploading audio to AzuraCast station {StationId}",
                podcastRequest.Id, station.ExternalStationId);

            // Download audio from ai-service URL, convert, and upload to AzuraCast
            var uploadResult = await _azuraCastPodcastService.UploadAndQueuePodcastAsync(
                station.ExternalStationId,
                podcastRequest.AudioUrl,
                podcastRequest.Title,
                cancellationToken);

            if (!uploadResult.IsSuccess)
            {
                _logger.LogError("Failed to upload podcast to AzuraCast: {Error}", uploadResult.ErrorMessage);
                return Result<PodcastRequestResult>.Failure(
                    uploadResult.ErrorMessage ?? "Failed to upload podcast to AzuraCast",
                    ErrorCode.InternalServerError);
            }

            podcastRequest.AzuraCastMediaId = uploadResult.MediaId;
            podcastRequest.Status = PodcastRequestStatus.Approved;

            _logger.LogInformation(
                "Podcast request {Id} approved and uploaded to AzuraCast (mediaId: {MediaId})",
                podcastRequest.Id, uploadResult.MediaId);
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
            RequestedAt = podcastRequest.RequestedAt
        });
    }
}