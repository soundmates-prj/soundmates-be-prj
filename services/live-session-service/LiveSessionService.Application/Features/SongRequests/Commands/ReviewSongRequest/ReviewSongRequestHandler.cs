using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.SongRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.SongRequests.Commands.ReviewSongRequest;

public sealed class ReviewSongRequestHandler : ICommandHandler<ReviewSongRequestCommand, SongRequestResult>
{
    private readonly ISongRequestRepository _songRequestRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ReviewSongRequestHandler> _logger;

    public ReviewSongRequestHandler(
        ISongRequestRepository songRequestRepository,
        IAzuraCastClient azuraCastClient,
        IDateTimeProvider dateTimeProvider,
        ILogger<ReviewSongRequestHandler> logger)
    {
        _songRequestRepository = songRequestRepository;
        _azuraCastClient = azuraCastClient;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<SongRequestResult>> Handle(ReviewSongRequestCommand command, CancellationToken cancellationToken)
    {
        var songRequest = await _songRequestRepository.GetByIdWithDetailsAsync(command.SongRequestId, cancellationToken);
        if (songRequest == null)
            return Result<SongRequestResult>.Failure("Song request not found", ErrorCode.NotFound);

        if (songRequest.Status != SongRequestStatus.Pending)
            return Result<SongRequestResult>.Failure("Song request has already been reviewed", ErrorCode.BadRequest);

        if (command.IsApproved)
        {
            var station = songRequest.LiveSession.AzuraCastStation;
            if (station == null)
                return Result<SongRequestResult>.Failure("Live session station not found", ErrorCode.BadRequest);

            await _azuraCastClient.QueueSongRequestAsync(
                station.ExternalStationId,
                songRequest.MediaFile.FilePath,
                cancellationToken);

            songRequest.Status = SongRequestStatus.Approved;

            _logger.LogInformation(
                "Approved song request {SongRequestId} and synced to AzuraCast station {StationId}",
                songRequest.Id,
                station.Id);
        }
        else
        {
            songRequest.Status = SongRequestStatus.Rejected;
            songRequest.RejectReason = command.RejectReason;
        }

        songRequest.ReviewedByUserId = command.ReviewedByUserId;
        songRequest.ReviewedAt = _dateTimeProvider.UtcNow;

        await _songRequestRepository.UpdateAsync(songRequest, cancellationToken);

        return Result<SongRequestResult>.Success(new SongRequestResult
        {
            Id = songRequest.Id,
            LiveSessionId = songRequest.LiveSessionId,
            MediaFileId = songRequest.MediaFileId,
            RequestedByUserId = songRequest.RequestedByUserId,
            Status = songRequest.Status.ToString(),
            ReviewedByUserId = songRequest.ReviewedByUserId,
            RequestedAt = songRequest.RequestedAt,
            ReviewedAt = songRequest.ReviewedAt,
            Message = songRequest.Message,
            RejectReason = songRequest.RejectReason,
            SongTitle = songRequest.MediaFile.Title,
            SongArtist = songRequest.MediaFile.Artist,
            SongAlbum = songRequest.MediaFile.Album
        });
    }
}
