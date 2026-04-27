using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers;

public sealed class UserActivatedEventHandler : UserEventHandlerBase
{
    // 1 = Active, 2 = Deactivated, 3 = Suspended, 4 = PendingDeletion
    private const int ActiveStatus = 1;

    public override string EventType => "auth.user.activated";

    public UserActivatedEventHandler(IUserReadRepository repository, ILogger<UserActivatedEventHandler> logger)
        : base(repository, logger) { }

    protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
    {
        var userId = GetUserId(root);
        _logger.LogInformation("Processing user activated event: {UserId}", userId);

        var existing = await _repository.GetByIdAsync(userId);
        if (existing is null)
        {
            _logger.LogWarning("User not found in MongoDB for activated event: {UserId}", userId);
            return;
        }

        existing.IsActive = true;
        existing.AccountStatus = ActiveStatus;
        // Clear deactivation / deletion fields when user is reactivated
        existing.DeactivatedAt = null;
        existing.DeactivationReason = null;
        existing.BannedAt = null;
        existing.BanReason = null;
        existing.IsBanned = false;
        existing.DeletionRequestedAt = null;
        existing.DeletionScheduledAt = null;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpsertAsync(existing);
        _logger.LogInformation("User marked as ACTIVATED in MongoDB: {UserId}", userId);
    }
}
