using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserUnbannedEventHandler : UserEventHandlerBase
    {
        // 1 = Active, 2 = Deactivated, 3 = Suspended, 4 = PendingDeletion
        private const int ActiveStatus = 1;

        public override string EventType => "auth.user.unbanned";

        public UserUnbannedEventHandler(IUserReadRepository repository, ILogger<UserUnbannedEventHandler> logger)
            : base(repository, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = GetUserId(root);
            _logger.LogInformation("Processing user unbanned event: {UserId}", userId);

            var existing = await _repository.GetByIdAsync(userId);
            if (existing is null)
            {
                _logger.LogWarning("User not found in MongoDB for unbanned event: {UserId}", userId);
                return;
            }

            existing.IsActive = true;
            existing.AccountStatus = ActiveStatus;
            existing.IsBanned = false;
            existing.BannedAt = null;
            existing.BanReason = null;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpsertAsync(existing);
            _logger.LogInformation("User marked as UNBANNED (active) in MongoDB: {UserId}", userId);
        }
    }
}
