using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Api.Hubs;

public sealed class LiveSessionHub : Hub
{
    private readonly LiveSessionDbContext _dbContext;
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LiveSessionHub(
        LiveSessionDbContext dbContext,
        ILiveSessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _sessionRepository = sessionRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task JoinSession(Guid sessionId, Guid? userId = null, string? anonymousIdentifier = null)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, Context.ConnectionAborted);
        if (session == null)
        {
            throw new HubException("Session not found");
        }

        if (session.Status != SessionStatus.Live && session.Status != SessionStatus.Paused)
        {
            throw new HubException("Session is not active");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroup(sessionId));

        var now = _dateTimeProvider.UtcNow;
        var listener = await FindExistingListenerAsync(sessionId, userId, anonymousIdentifier, Context.ConnectionAborted);

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
            listener.IsConnected = true;
            listener.DisconnectedAt = null;
            listener.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(Context.ConnectionAborted);

        var currentListeners = await _dbContext.SessionListeners
            .CountAsync(x => x.LiveSessionId == sessionId && x.IsConnected, Context.ConnectionAborted);

        await Clients.Group(GetSessionGroup(sessionId))
            .SendAsync("ListenerCountUpdated", sessionId, currentListeners, Context.ConnectionAborted);
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

        var currentListeners = await _dbContext.SessionListeners
            .CountAsync(x => x.LiveSessionId == sessionId && x.IsConnected, Context.ConnectionAborted);

        await Clients.Group(GetSessionGroup(sessionId))
            .SendAsync("ListenerCountUpdated", sessionId, currentListeners, Context.ConnectionAborted);
    }

    public async Task ReconnectSession(Guid sessionId, Guid? userId = null, string? anonymousIdentifier = null)
    {
        await JoinSession(sessionId, userId, anonymousIdentifier);
        await Clients.Caller.SendAsync("SessionReconnected", sessionId, Context.ConnectionAborted);
    }

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

        await Clients.Group(GetSessionGroup(sessionId)).SendAsync("ChatMessageReceived", new
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
