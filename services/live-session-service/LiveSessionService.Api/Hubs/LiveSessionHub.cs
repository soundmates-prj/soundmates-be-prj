using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Api.Hubs;

public sealed class LiveSessionHub : Hub
{
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

    public async Task ReconnectSession(Guid sessionId, Guid? userId = null, string? anonymousIdentifier = null)
    {
        await JoinSession(sessionId, userId, anonymousIdentifier);
        await Clients.Caller.SendAsync("SessionReconnected", sessionId, Context.ConnectionAborted);
    }

    public async Task SendChat(Guid sessionId, Guid userId, string message)
        => await SendMessage(sessionId, userId, message);

    public async Task SendMessage(Guid sessionId, Guid userId, string message)
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
            chat.Message,
            chat.CreatedAt
        }, Context.ConnectionAborted);
    }

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