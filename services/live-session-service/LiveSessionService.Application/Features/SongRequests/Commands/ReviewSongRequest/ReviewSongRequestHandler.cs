using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.SongRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using shared.Contracts.Events.Notifications;
using System.Text.Json;

namespace LiveSessionService.Application.Features.SongRequests.Commands.ReviewSongRequest;

public sealed class ReviewSongRequestHandler : ICommandHandler<ReviewSongRequestCommand, SongRequestResult>
{
    private readonly ISongRequestRepository _songRequestRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMessageBusPublisher _eventBus;
    private readonly ILogger<ReviewSongRequestHandler> _logger;

    public ReviewSongRequestHandler(
        ISongRequestRepository songRequestRepository,
        IAzuraCastClient azuraCastClient,
        IDateTimeProvider dateTimeProvider,
        IMessageBusPublisher eventBus,
        ILogger<ReviewSongRequestHandler> logger)
    {
        _songRequestRepository = songRequestRepository;
        _azuraCastClient = azuraCastClient;
        _dateTimeProvider = dateTimeProvider;
        _eventBus = eventBus;
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

            if (string.IsNullOrWhiteSpace(songRequest.MediaFile.AzuraCastMediaId))
                return Result<SongRequestResult>.Failure("Media file has not been synced to AzuraCast yet", ErrorCode.BadRequest);

            try 
            {
                await _azuraCastClient.QueueSongRequestAsync(
                    station.ExternalStationId,
                    songRequest.MediaFile.AzuraCastMediaId,
                    cancellationToken);

                songRequest.Status = SongRequestStatus.Approved;

                _logger.LogInformation(
                    "Approved song request {SongRequestId} and synced to AzuraCast station {StationId}",
                    songRequest.Id,
                    station.Id);
            }
            catch (Exception ex) when (ex.Message.Contains("recently") || ex.Message.Contains("Wait a while"))
            {
                _logger.LogWarning(ex, "AzuraCast rejected song request {SongRequestId}: {Message}", songRequest.Id, ex.Message);
                
                // Nếu AzuraCast báo bài hát đã được phát gần đây, tự động chuyển sang trạng thái Reject
                songRequest.Status = SongRequestStatus.Rejected;
                songRequest.RejectReason = "Bài hát này đã được phát gần đây. Vui lòng thử lại sau.";
            }
            catch (Exception ex) when (ex.Message.Contains("not requestable", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(ex, "AzuraCast rejected song request {SongRequestId} as not requestable. Forcing assignment to a requestable playlist.", songRequest.Id);
                try 
                {
                    var playlists = await _azuraCastClient.GetStationPlaylistsAsync(station.ExternalStationId, cancellationToken);
                    var reqPlaylist = playlists.FirstOrDefault(p => p.IncludeInRequests);
                    
                    if (reqPlaylist != null)
                    {
                        await _azuraCastClient.AssignMediaToPlaylistAsync(
                            station.ExternalStationId, 
                            songRequest.MediaFile.AzuraCastMediaId, 
                            reqPlaylist.Id, 
                            cancellationToken);
                            
                        await Task.Delay(500, cancellationToken); // Wait a bit for AzuraCast to sync DB
                        
                        await _azuraCastClient.QueueSongRequestAsync(
                            station.ExternalStationId,
                            songRequest.MediaFile.AzuraCastMediaId,
                            cancellationToken);
                            
                        songRequest.Status = SongRequestStatus.Approved;
                        _logger.LogInformation("Successfully force-queued song request {SongRequestId} via playlist {PlaylistId}", songRequest.Id, reqPlaylist.Id);
                    }
                    else 
                    {
                        songRequest.Status = SongRequestStatus.Rejected;
                        songRequest.RejectReason = "Không tìm thấy playlist nào có bật tính năng Request trên hệ thống AzuraCast.";
                    }
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "Failed to force-queue song request {SongRequestId} to AzuraCast after assigning playlist", songRequest.Id);
                    songRequest.Status = SongRequestStatus.Rejected;
                    songRequest.RejectReason = "Trạm phát (AzuraCast) từ chối yêu cầu và thủ thuật ép phát cũng thất bại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue song request {SongRequestId} to AzuraCast", songRequest.Id);
                return Result<SongRequestResult>.Failure(ex.Message, ErrorCode.InternalServerError);
            }
        }
        else
        {
            songRequest.Status = SongRequestStatus.Rejected;
            songRequest.RejectReason = command.RejectReason;
        }

        songRequest.ReviewedByUserId = command.ReviewedByUserId;
        songRequest.ReviewedAt = _dateTimeProvider.UtcNow;

        await _songRequestRepository.UpdateAsync(songRequest, cancellationToken);

        // Notify the member about the review result
        try
        {
            var songTitle = string.IsNullOrWhiteSpace(songRequest.MediaFile.Artist)
                ? songRequest.MediaFile.Title
                : $"{songRequest.MediaFile.Title} - {songRequest.MediaFile.Artist}";

            string notificationTitle;
            string notificationMessage;

            if (songRequest.Status == SongRequestStatus.Approved)
            {
                notificationTitle = "Yêu cầu bài hát được chấp nhận";
                notificationMessage = $"Yêu cầu bài '{songTitle}' của bạn đã được host chấp nhận và sẽ được phát sớm!";
            }
            else
            {
                var reason = string.IsNullOrWhiteSpace(songRequest.RejectReason)
                    ? "Không có lý do"
                    : songRequest.RejectReason;
                notificationTitle = "Yêu cầu bài hát bị từ chối";
                notificationMessage = $"Yêu cầu bài '{songTitle}' của bạn đã bị từ chối. Lý do: {reason}";
            }

            var notifyMemberEvent = new NotificationEvent
            {
                Title = notificationTitle,
                SendUserId = command.ReviewedByUserId,
                ReceiveUserId = songRequest.RequestedByUserId,
                ReferenceId = songRequest.Id,
                Type = "song_request_review",
                Message = notificationMessage,
                IsBroadcast = false
            };

            await _eventBus.PublishAsync(
                "notification.created",
                JsonSerializer.Serialize(notifyMemberEvent),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Fire-and-forget: notification failure should not block the review result
            _logger.LogError(ex, "Failed to publish song request review notification for {SongRequestId}", songRequest.Id);
        }

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
