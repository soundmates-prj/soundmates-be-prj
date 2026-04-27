using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    /// <summary>
    /// Handles user deletion events.
    /// - DeletionRequested with future ScheduledAt: sets AccountStatus=4 (PendingDeletion) + deletion fields
    /// - After grace period / no ScheduledAt: hard deletes user from read model
    /// </summary>
    public sealed class UserDeletedEventHandler : UserEventHandlerBase
    {
        // 1 = Active, 2 = Deactivated, 3 = Suspended, 4 = PendingDeletion
        private const int PendingDeletionStatus = 4;

        public UserDeletedEventHandler(IUserReadRepository repository, ILogger<UserDeletedEventHandler> logger)
            : base(repository, logger) { }

        public override string EventType => "auth.user.deleted";

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            // Extract userId from the event root (UserDeletedEvent has "id" property)
            Guid userId = EventPropertyExtractor.GetNullableGuidProperty(root, "id", "Id", "userId", "UserId")
                         ?? EventPropertyExtractor.GetGuidProperty(root, "id", "Id");

            _logger.LogDebug("Processing deletion event for user: {UserId}", userId);

            // Get DeletionScheduledAt from event
            var deletionScheduledAt = EventPropertyExtractor.GetDateTimeProperty(root, "deletedAt", "DeletedAt")
                                   ?? EventPropertyExtractor.GetDateTimeProperty(root, "deletionScheduledAt", "DeletionScheduledAt");

            // If a scheduled deletion date is in the future, treat as soft mark (pending deletion)
            if (deletionScheduledAt.HasValue && deletionScheduledAt.Value > DateTime.UtcNow)
            {
                // Soft mark: update the read model with deletion fields + canonical accountStatus
                var existing = await _repository.GetByIdAsync(userId);
                if (existing != null)
                {
                    existing.DeletionRequestedAt = DateTime.UtcNow;
                    existing.DeletionScheduledAt = deletionScheduledAt.Value;
                    existing.IsActive = false;
                    existing.AccountStatus = PendingDeletionStatus;
                    existing.UpdatedAt = DateTime.UtcNow;

                    await _repository.UpsertAsync(existing);
                    _logger.LogInformation(
                        "User {UserId} marked for deletion (grace period until {ScheduledDate})",
                        userId, deletionScheduledAt.Value);
                }
                else
                {
                    _logger.LogWarning(
                        "User {UserId} not found in MongoDB for soft deletion mark",
                        userId);
                }
            }
            else
            {
                // Hard delete: remove user from read model
                await _repository.DeleteAsync(userId);
                _logger.LogInformation("User hard deleted from MongoDB: {UserId}", userId);
            }
        }
    }
}
