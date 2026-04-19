using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LiveSessionService.Api.Hubs;

public sealed class LiveSessionHub : Hub
{
    // sessionId → connectionId of the host currently broadcasting mic
    private static readonly ConcurrentDictionary<string, string> _micHosts = new();

    // sessionId → current global volume set by the host (default starts at 1.0)
    private static readonly ConcurrentDictionary<string, double> _sessionVolumes = new();

    // connectionId → (sessionId, userId, anonymousIdentifier) to cleanup on disconnect
    private static readonly ConcurrentDictionary<string, (Guid SessionId, Guid? UserId, string? AnonymousId)> _connections = new();

    private readonly LiveSessionDbContext _dbContext;
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LiveSessionHub> _logger;

    public LiveSessionHub(
        LiveSessionDbContext dbContext,
        ILiveSessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<LiveSessionHub> logger)
    {
        _dbContext = dbContext;
        _sessionRepository = sessionRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    private async Task<int> CountConnectedListenersAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var connected = await _dbContext.SessionListeners
            .Where(x => x.LiveSessionId == sessionId && x.IsConnected)
            .Select(x => new { x.UserId, x.AnonymousIdentifier, x.Id })
            .ToListAsync(cancellationToken);

        return connected
            .Select(x => x.UserId.HasValue
                ? $"u:{x.UserId.Value}"
                : $"a:{x.AnonymousIdentifier ?? x.Id.ToString()}")
            .Distinct()
            .Count();
    }

    public async Task JoinSession(Guid sessionId, Guid? userId = null, string? anonymousIdentifier = null)
    {
        try
        {
            // 1. Load session
            LiveSession? session;
            try
            {
                session = await _sessionRepository.GetByIdAsync(sessionId, Context.ConnectionAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[JoinSession] DB error loading session {SessionId}", sessionId);
                throw new HubException($"Database error loading session: {ex.Message}");
            }

            if (session == null)
            {
                throw new HubException("Session not found");
            }

            _logger.LogInformation(
                "[JoinSession] SessionId={SessionId}, Status={Status}, HostUserId={HostUserId}, AzuraCastStationId={StationId}, UserId={UserId}, AnonymousId={AnonymousId}",
                sessionId, session.Status, session.HostUserId, session.AzuraCastStationId, userId, anonymousIdentifier);

            if (session.Status != SessionStatus.Live && session.Status != SessionStatus.Paused)
            {
                throw new HubException($"Session is not active. Current status: {session.Status}");
            }

            // 2. Add to SignalR group
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroup(sessionId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[JoinSession] Failed to add connection {ConnectionId} to group {SessionId}",
                    Context.ConnectionId, sessionId);
                throw new HubException($"Failed to join session group: {ex.Message}");
            }

            // 3. Find or create listener
            var now = _dateTimeProvider.UtcNow;
            SessionListener listener;
            try
            {
                listener = (await FindExistingListenerAsync(sessionId, userId, anonymousIdentifier, Context.ConnectionAborted))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[JoinSession] DB error finding listener for {SessionId}", sessionId);
                throw new HubException($"Database error finding listener: {ex.Message}");
            }

            if (listener == null)
            {
                listener = new SessionListener
                {
                    Id = Guid.NewGuid(),
                    LiveSessionId = sessionId,
                    UserId = userId,
                    AnonymousIdentifier = anonymousIdentifier,
                    IpAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Context.GetHttpContext()?.Request.Headers.UserAgent.ToString(),
                    ConnectedAt = now,
                    IsConnected = true,
                    DurationSeconds = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.SessionListeners.Add(listener);
            }
            else
            {
                // Upgrade guest to authenticated user: if listener has no UserId but the caller is now logged in,
                // promote them so they stop being treated as a guest and won't get kicked by the 2-min timeout.
                if (listener.UserId == null && userId.HasValue && userId.Value != Guid.Empty)
                {
                    _logger.LogInformation("[JoinSession] Upgrading guest to authenticated user. AnonymousId={AnonymousId}, UserId={UserId}",
                        anonymousIdentifier, userId.Value);
                    listener.UserId = userId.Value;
                    listener.AnonymousIdentifier = null;
                }

                // IMPORTANT: reset connection start time on re-join so timeout/duration are based
                // on the current watch session, not a stale ConnectedAt from previous visits.
                listener.ConnectedAt = now;
                listener.IsConnected = true;
                listener.DisconnectedAt = null;
                listener.DurationSeconds = 0;
                listener.UpdatedAt = now;
            }

            // 4. Save
            try
            {
                await _dbContext.SaveChangesAsync(Context.ConnectionAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[JoinSession] DB error saving listener for {SessionId}", sessionId);
                throw new HubException($"Database error saving listener: {ex.Message}");
            }

            // 5. Count listeners
            int currentListeners;
            try
            {
                currentListeners = await CountConnectedListenersAsync(sessionId, Context.ConnectionAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[JoinSession] DB error counting listeners for {SessionId}", sessionId);
                throw new HubException($"Database error counting listeners: {ex.Message}");
            }

            // 6. Broadcast
            await Clients.Group(GetSessionGroup(sessionId))
                .SendAsync("ListenersUpdated", sessionId, currentListeners, Context.ConnectionAborted);

            await Clients.Group(GetSessionGroup(sessionId))
                .SendAsync("UserJoined", sessionId, userId, currentListeners, Context.ConnectionAborted);

            _logger.LogInformation("[JoinSession] Success. SessionId={SessionId}, Listeners={Listeners}", sessionId, currentListeners);

            // Register connection for cleanup
            _connections[Context.ConnectionId] = (sessionId, userId, anonymousIdentifier);

            // 7. Send Chat History to the new listener
            try
            {
                var history = await _dbContext.LiveSessionChats
                    .Where(c => c.LiveSessionId == sessionId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(50)
                    .Select(c => new
                    {
                        c.Id,
                        c.LiveSessionId,
                        c.UserId,
                        UserName = c.UserName ?? "Ẩn danh",
                        AvatarUrl = c.AvatarUrl,
                        c.Message,
                        c.CreatedAt
                    })
                    .ToListAsync(Context.ConnectionAborted);

                history.Reverse();
                await Clients.Caller.SendAsync("ChatHistory", history, Context.ConnectionAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[JoinSession] Failed to fetch chat history for {SessionId}", sessionId);
            }

            // 8. Sync Live Ephemeral State (Mic & Volume) to the new listener
            var sessionIdStr = sessionId.ToString();
            
            if (_micHosts.ContainsKey(sessionIdStr))
            {
                await Clients.Caller.SendAsync("HostMicStarted", sessionIdStr, Context.ConnectionAborted);
            }
            
            if (_sessionVolumes.TryGetValue(sessionIdStr, out var currentVolume))
            {
                await Clients.Caller.SendAsync("GlobalVolumeUpdated", currentVolume, Context.ConnectionAborted);
            }
        }
        catch (HubException)
        {
            throw; // Re-throw HubException as-is (SignalR propagates message to client)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JoinSession] Unexpected error for SessionId={SessionId}", sessionId);
            throw new HubException($"Internal error joining session: {ex.Message}");
        }
    }

    public async Task LeaveSession(Guid sessionId, Guid? userId = null, string? anonymousIdentifier = null)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSessionGroup(sessionId));

        var listener = await FindExistingListenerAsync(sessionId, userId, anonymousIdentifier, Context.ConnectionAborted);
        if (listener != null && listener.IsConnected)
        {
            var now = _dateTimeProvider.UtcNow;
            listener.IsConnected = false;
            listener.DisconnectedAt = now;
            listener.UpdatedAt = now;
            listener.DurationSeconds = Math.Max(0, (int)(now - listener.ConnectedAt).TotalSeconds);
            await _dbContext.SaveChangesAsync(Context.ConnectionAborted);
        }

        var currentListeners = await CountConnectedListenersAsync(sessionId, Context.ConnectionAborted);

        await Clients.Group(GetSessionGroup(sessionId))
            .SendAsync("ListenersUpdated", sessionId, currentListeners, Context.ConnectionAborted);

        await Clients.Group(GetSessionGroup(sessionId))
            .SendAsync("UserLeft", sessionId, userId, currentListeners, Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connections.TryRemove(Context.ConnectionId, out var data))
        {
            try
            {
                await LeaveSession(data.SessionId, data.UserId, data.AnonymousId);
                _logger.LogInformation("[OnDisconnectedAsync] Auto cleaned up listener {ConnId} for Session {SessionId}",
                    Context.ConnectionId, data.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[OnDisconnectedAsync] Failed to auto LeaveSession for {ConnId}", Context.ConnectionId);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task ReconnectSession(Guid sessionId, Guid? userId = null, string? anonymousIdentifier = null)
    {
        await JoinSession(sessionId, userId, anonymousIdentifier);
        await Clients.Caller.SendAsync("SessionReconnected", sessionId, Context.ConnectionAborted);
    }

    public async Task SendChat(Guid sessionId, Guid userId, string message, string? userName = null, string? avatarUrl = null)
        => await SendMessage(sessionId, userId, message, userName, avatarUrl);

    public async Task SendMessage(Guid sessionId, Guid userId, string message, string? userName = null, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Message cannot be empty");
        }

        var session = await _sessionRepository.GetByIdAsync(sessionId, Context.ConnectionAborted);
        if (session == null)
        {
            throw new HubException("Session not found");
        }

        if (session.Status == SessionStatus.Ended || session.Status == SessionStatus.Cancelled)
        {
            throw new HubException("Session has ended");
        }

        var chat = new LiveSessionChat
        {
            Id = Guid.NewGuid(),
            LiveSessionId = sessionId,
            UserId = userId,
            UserName = userName,
            AvatarUrl = avatarUrl,
            Message = message.Trim(),
            CreatedAt = _dateTimeProvider.UtcNow
        };

        _dbContext.LiveSessionChats.Add(chat);
        await _dbContext.SaveChangesAsync(Context.ConnectionAborted);

        await Clients.Group(GetSessionGroup(sessionId)).SendAsync("ReceiveChat", new
        {
            chat.Id,
            chat.LiveSessionId,
            chat.UserId,
            UserName = chat.UserName ?? "Ẩn danh",
            AvatarUrl = chat.AvatarUrl,
            chat.Message,
            chat.CreatedAt
        }, Context.ConnectionAborted);
    }

    public async Task DeleteChat(Guid sessionId, Guid chatId, Guid requestUserId, string requestUserRole)
    {
        var chat = await _dbContext.LiveSessionChats.FirstOrDefaultAsync(c => c.Id == chatId, Context.ConnectionAborted);
        if (chat == null) return; // NotFound

        // Validation: Must be host, staff, or the sender themselves
        bool isStaffOrHost = requestUserRole == "Host" || requestUserRole == "Staff" || requestUserRole == "Admin";
        if (!isStaffOrHost && chat.UserId != requestUserId)
        {
            throw new HubException("Bạn không có quyền xóa tin nhắn này.");
        }

        // We completely delete from database to match 'xóa không để lại dấu vết' option
        _dbContext.LiveSessionChats.Remove(chat);
        await _dbContext.SaveChangesAsync(Context.ConnectionAborted);

        // Broadcast completely removed event
        await Clients.Group(GetSessionGroup(sessionId)).SendAsync("ChatDeleted", chatId, Context.ConnectionAborted);
    }

    // ─── WebRTC Signaling ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the Host when they start broadcasting their microphone.
    /// Stores host connectionId and broadcasts "HostMicStarted" to all listeners.
    /// sdpOffer is NOT sent in the broadcast — each listener will call ListenerAnswerMic
    /// to do individual peer-to-peer negotiation.
    /// </summary>
    public async Task StartMicrophone(Guid sessionId, string sdpOffer)
    {
        var sessionIdStr = sessionId.ToString();
        _micHosts[sessionIdStr] = Context.ConnectionId;
        _logger.LogInformation("[StartMicrophone] Host {ConnId} started mic for session {SessionId}", Context.ConnectionId, sessionId);

        // Broadcast to all listeners that host mic is now live
        await Clients
            .GroupExcept(GetSessionGroup(sessionId), Context.ConnectionId)
            .SendAsync("HostMicStarted", sessionIdStr, Context.ConnectionAborted);
    }

    /// <summary>
    /// Called by the Host when they stop broadcasting their microphone.
    /// Broadcasts "HostMicStopped" to all listeners.
    /// </summary>
    public async Task StopMicrophone(Guid sessionId)
    {
        var sessionIdStr = sessionId.ToString();
        _micHosts.TryRemove(sessionIdStr, out _);
        _logger.LogInformation("[StopMicrophone] Host {ConnId} stopped mic for session {SessionId}", Context.ConnectionId, sessionId);

        await Clients
            .GroupExcept(GetSessionGroup(sessionId), Context.ConnectionId)
            .SendAsync("HostMicStopped", sessionIdStr, Context.ConnectionAborted);
    }

    /// <summary>
    /// Called by a Listener when they want to receive the host mic stream.
    /// Sends the listener's SDP Offer to the host so the host can create an Answer.
    /// </summary>
    public async Task ListenerRequestMic(Guid sessionId, string sdpOffer)
    {
        var sessionIdStr = sessionId.ToString();
        if (!_micHosts.TryGetValue(sessionIdStr, out var hostConnId))
        {
            throw new HubException("Host is not currently broadcasting microphone");
        }

        // Relay listener's SDP offer to the host
        await Clients.Client(hostConnId).SendAsync(
            "ListenerWantsToSubscribe",
            sessionIdStr,
            Context.ConnectionId,
            sdpOffer,
            Context.ConnectionAborted);
    }

    /// <summary>
    /// Called by the Host to send SDP Answer back to a specific listener.
    /// </summary>
    public async Task HostAnswerListener(Guid sessionId, string listenerConnectionId, string sdpAnswer)
    {
        _logger.LogDebug("[HostAnswerListener] Session {SessionId} → Listener {ListenerConnId}", sessionId, listenerConnectionId);
        await Clients.Client(listenerConnectionId).SendAsync(
            "ReceiveHostAnswer",
            sessionId.ToString(),
            sdpAnswer,
            Context.ConnectionAborted);
    }

    /// <summary>
    /// Relay ICE candidates between host and listeners (both directions).
    /// targetConnectionId = the specific peer to forward the candidate to.
    /// </summary>
    public async Task IceCandidateRelay(Guid sessionId, string targetConnectionId, string candidate)
    {
        var sessionIdStr = sessionId.ToString();
        var relayTarget = targetConnectionId;

        // If target is empty, assume it's sent from a listener to the host
        if (string.IsNullOrEmpty(relayTarget))
        {
            if (_micHosts.TryGetValue(sessionIdStr, out var hostConnId))
            {
                relayTarget = hostConnId;
            }
            else
            {
                return;
            }
        }

        _logger.LogDebug("[IceCandidateRelay] Session {SessionId}: {From} → {To}", sessionId, Context.ConnectionId, relayTarget);
        await Clients.Client(relayTarget).SendAsync(
            "ReceiveIceCandidate",
            sessionIdStr,
            candidate,
            Context.ConnectionAborted);
    }

    public async Task HostUpdateGlobalVolume(Guid sessionId, double volume)
    {
        var sessionIdStr = sessionId.ToString();
        _sessionVolumes[sessionIdStr] = volume;

        // Broadcast the volume adjustment to all listeners
        await Clients.Group(GetSessionGroup(sessionId)).SendAsync(
            "GlobalVolumeUpdated",
            volume,
            Context.ConnectionAborted);
    }

    // ─── Private helpers ─────────────────────────────────────────────────────────

    private async Task<SessionListener?> FindExistingListenerAsync(
        Guid sessionId,
        Guid? userId,
        string? anonymousIdentifier,
        CancellationToken cancellationToken)
    {
        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            return await _dbContext.SessionListeners
                .Where(x => x.LiveSessionId == sessionId && x.UserId == userId.Value)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(anonymousIdentifier))
        {
            return await _dbContext.SessionListeners
                .Where(x => x.LiveSessionId == sessionId && x.AnonymousIdentifier == anonymousIdentifier)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private static string GetSessionGroup(Guid sessionId) => $"live-session-{sessionId}";
}