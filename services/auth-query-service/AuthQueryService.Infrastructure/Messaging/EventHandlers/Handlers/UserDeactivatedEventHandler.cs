using System.Text.Json;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserDeactivatedEventHandler : UserEventHandlerBase
    {
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
            existing.IsBanned = false; // Clear ban state if transitioning to deactivated
            existing.DeactivatedAt = EventPropertyExtractor.GetDateTimeProperty(root, "deactivatedAt", "DeactivatedAt");
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpsertAsync(existing);
            _logger.LogInformation("User marked as DEACTIVATED in MongoDB: {UserId}", userId);
        }
    }
}
