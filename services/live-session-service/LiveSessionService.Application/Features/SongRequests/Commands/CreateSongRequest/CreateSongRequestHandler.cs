using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.SongRequests;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.SongRequests.Commands.CreateSongRequest;

public sealed class CreateSongRequestHandler : ICommandHandler<CreateSongRequestCommand, SongRequestResult>
{
    private readonly ILiveSessionRepository _liveSessionRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly ISongRequestRepository _songRequestRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSongRequestHandler(
        ILiveSessionRepository liveSessionRepository,
        IMediaFileRepository mediaFileRepository,
        ISongRequestRepository songRequestRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _liveSessionRepository = liveSessionRepository;
        _mediaFileRepository = mediaFileRepository;
        _songRequestRepository = songRequestRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SongRequestResult>> Handle(CreateSongRequestCommand command, CancellationToken cancellationToken)
    {
        var liveSession = await _liveSessionRepository.GetByIdWithStationAsync(command.LiveSessionId, cancellationToken);
        if (liveSession == null)
            return Result<SongRequestResult>.Failure("Live session not found", ErrorCode.NotFound);

        if (liveSession.AzuraCastStationId == null)
            return Result<SongRequestResult>.Failure("Live session is not linked to a station", ErrorCode.BadRequest);

        var mediaFile = await _mediaFileRepository.GetByIdAsync(command.MediaFileId, cancellationToken);
        if (mediaFile == null)
            return Result<SongRequestResult>.Failure("Media file not found", ErrorCode.NotFound);

        var songRequest = new SongRequest
        {
            Id = Guid.NewGuid(),
            LiveSessionId = command.LiveSessionId,
            MediaFileId = command.MediaFileId,
            RequestedByUserId = command.RequestedByUserId,
            Status = SongRequestStatus.Pending,
            RequestedAt = _dateTimeProvider.UtcNow,
            Message = command.Message
        };

        await _songRequestRepository.AddAsync(songRequest, cancellationToken);

        return Result<SongRequestResult>.Success(new SongRequestResult
        {
            Id = songRequest.Id,
            LiveSessionId = songRequest.LiveSessionId,
            MediaFileId = songRequest.MediaFileId,
            RequestedByUserId = songRequest.RequestedByUserId,
            Status = songRequest.Status.ToString(),
            RequestedAt = songRequest.RequestedAt,
            Message = songRequest.Message,
            SongTitle = mediaFile.Title,
            SongArtist = mediaFile.Artist,
            SongAlbum = mediaFile.Album
        });
    }
}
