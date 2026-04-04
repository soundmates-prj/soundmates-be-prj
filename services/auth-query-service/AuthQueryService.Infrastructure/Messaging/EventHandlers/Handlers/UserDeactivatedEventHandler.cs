using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserDeactivatedEventHandler : UserEventHandlerBase
    {
        // 1 = Active, 2 = Deactivated, 3 = Suspended, 4 = PendingDeletion
        private const int DeactivatedStatus = 2;

        public override string EventType => "auth.user.deactivated";

        public UserDeactivatedEventHandler(IUserReadRepository repository, ILogger<UserDeactivatedEventHandler> logger)
            : base(repository, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = GetUserId(root);
            _logger.LogInformation("Processing user deactivated event: {UserId}", userId);

            var existing = await _repository.GetByIdAsync(userId);
            if (existing is null)
            {
                _logger.LogWarning("User not found in MongoDB for deactivated event: {UserId}", userId);
                return;
            }

            existing.IsActive = false;
            existing.AccountStatus = DeactivatedStatus;
            existing.IsBanned = false; // Clear ban state if transitioning to deactivated
            existing.DeactivatedAt = EventPropertyExtractor.GetDateTimeProperty(root, "deactivatedAt", "DeactivatedAt");
            existing.DeactivationReason = EventPropertyExtractor.GetOptionalStringProperty(root, "reason", "Reason")
                                       ?? EventPropertyExtractor.GetOptionalStringProperty(root, "deactivationReason", "DeactivationReason");
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpsertAsync(existing);
            _logger.LogInformation("User marked as DEACTIVATED in MongoDB: {UserId}", userId);
        }
    }
}
