using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.SongRequests;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using shared.Contracts.Events.Notifications;
using System.Text.Json;

namespace LiveSessionService.Application.Features.SongRequests.Commands.CreateSongRequest;

public sealed class CreateSongRequestHandler : ICommandHandler<CreateSongRequestCommand, SongRequestResult>
{
    private readonly ILiveSessionRepository _liveSessionRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly ISongRequestRepository _songRequestRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMessageBusPublisher _eventBus;
    private readonly IAccountContentClient _accountClient;

    public CreateSongRequestHandler(
        ILiveSessionRepository liveSessionRepository,
        IMediaFileRepository mediaFileRepository,
        ISongRequestRepository songRequestRepository,
        IDateTimeProvider dateTimeProvider,
        IMessageBusPublisher eventBus,
        IAccountContentClient accountClient)
    {
        _liveSessionRepository = liveSessionRepository;
        _mediaFileRepository = mediaFileRepository;
        _songRequestRepository = songRequestRepository;
        _dateTimeProvider = dateTimeProvider;
        _eventBus = eventBus;
        _accountClient = accountClient;
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

        // 1. Fetch user's subscription limits
        var subscription = await _accountClient.GetMySubscriptionFullAsync(command.UserToken, cancellationToken);
        var requestLimit = subscription?.RequestLimit ?? 0;

        // 2. Prevent Free members (limit 0) from requesting
        if (requestLimit == 0)
        {
            return Result<SongRequestResult>.Failure("Tài khoản hiện tại của bạn không hỗ trợ yêu cầu nhạc. Vui lòng nâng cấp gói.", ErrorCode.BadRequest);
        }

        // 3. Prevent users who have exceeded their daily limits
        var todayCount = await _songRequestRepository.CountRequestsByUserTodayAsync(command.RequestedByUserId, cancellationToken);
        if (todayCount >= requestLimit)
        {
            return Result<SongRequestResult>.Failure($"Bạn đã đạt giới hạn yêu cầu nhạc trong ngày ({todayCount}/{requestLimit} bài).", ErrorCode.BadRequest);
        }

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

        // Notify host that a member has requested a song
        try
        {
            var songTitle = string.IsNullOrWhiteSpace(mediaFile.Artist)
                ? mediaFile.Title
                : $"{mediaFile.Title} - {mediaFile.Artist}";

            var notifyHostEvent = new NotificationEvent
            {
                Title = "Yêu cầu bài hát mới",
                SendUserId = command.RequestedByUserId,
                ReceiveUserId = liveSession.HostUserId,
                ReferenceId = songRequest.Id,
                Type = "song_request",
                Message = $"Thành viên đã yêu cầu bài '{songTitle}'. Nhấn vào để xem yêu cầu.",
                IsBroadcast = false
            };

            await _eventBus.PublishAsync(
                "notification.created",
                JsonSerializer.Serialize(notifyHostEvent),
                cancellationToken);
        }
        catch (Exception)
        {
            // Fire-and-forget: notification failure should not block the request
        }

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
