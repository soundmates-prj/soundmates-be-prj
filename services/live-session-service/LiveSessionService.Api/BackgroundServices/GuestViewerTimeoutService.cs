using LiveSessionService.Api.Hubs;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Api.BackgroundServices;

/// <summary>
/// Background service that monitors guest viewers and enforces 2-minute viewing limit.
/// After 2 minutes, guests must login to continue watching.
/// </summary>
public sealed class GuestViewerTimeoutService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GuestViewerTimeoutService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30); // Check every 30 seconds
    private readonly TimeSpan _guestViewLimit = TimeSpan.FromMinutes(2); // 2 minutes free viewing

    public GuestViewerTimeoutService(
        IServiceProvider serviceProvider,
        ILogger<GuestViewerTimeoutService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GuestViewerTimeoutService started. Checking every {Interval} seconds", _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckGuestViewersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking guest viewers");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckGuestViewersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveSessionDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<LiveSessionHub>>();

        var now = DateTime.UtcNow;
        var timeoutThreshold = now.Subtract(_guestViewLimit);

        // Find all connected guest viewers who have exceeded the 2-minute limit
        var expiredGuests = await dbContext.SessionListeners
            .Where(x => 
                x.IsConnected && 
                x.UserId == null && // Guest (no user ID)
                x.ConnectedAt <= timeoutThreshold) // Connected more than 2 minutes ago
            .ToListAsync(cancellationToken);

        if (expiredGuests.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} guest viewers who exceeded 2-minute limit", expiredGuests.Count);

        foreach (var guest in expiredGuests)
        {
            try
            {
                // Send notification to guest that they need to login
                await hubContext.Clients
                    .Group($"live-session-{guest.LiveSessionId}")
                    .SendAsync("GuestViewLimitExceeded", new
                    {
                        SessionId = guest.LiveSessionId,
                        AnonymousIdentifier = guest.AnonymousIdentifier,
                        Message = "You've reached the 2-minute viewing limit. Please login to continue watching.",
                        ViewedDuration = (int)(now - guest.ConnectedAt).TotalSeconds
                    }, cancellationToken);

                // Mark guest as disconnected
                guest.IsConnected = false;
                guest.DisconnectedAt = now;
                guest.UpdatedAt = now;
                guest.DurationSeconds = (int)(now - guest.ConnectedAt).TotalSeconds;

                _logger.LogInformation(
                    "Guest viewer {AnonymousId} disconnected from session {SessionId} after {Duration} seconds",
                    guest.AnonymousIdentifier,
                    guest.LiveSessionId,
                    guest.DurationSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error disconnecting guest {AnonymousId} from session {SessionId}",
                    guest.AnonymousIdentifier,
                    guest.LiveSessionId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Update listener counts for affected sessions
        var affectedSessions = expiredGuests.Select(x => x.LiveSessionId).Distinct();
        foreach (var sessionId in affectedSessions)
        {
            var hostUserId = await dbContext.LiveSessions
                .Where(s => s.Id == sessionId)
                .Select(s => s.HostUserId)
                .FirstOrDefaultAsync(cancellationToken);

            var currentListeners = await dbContext.SessionListeners
                .Where(x => x.LiveSessionId == sessionId 
                         && x.IsConnected
                         && x.UserId.HasValue
                         && x.UserId.Value != hostUserId)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            await hubContext.Clients
                .Group($"live-session-{sessionId}")
                .SendAsync("ListenersUpdated", sessionId, currentListeners, cancellationToken);
        }
    }
}
