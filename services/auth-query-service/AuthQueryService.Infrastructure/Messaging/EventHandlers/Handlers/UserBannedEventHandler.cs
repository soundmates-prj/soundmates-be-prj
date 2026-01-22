using System.Text.Json;
using AuthQueryService.Infrastructure.DAO.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserBannedEventHandler : UserEventHandlerBase
    {
        public override string EventType => "auth.user.banned";

        public UserBannedEventHandler(IUserReadDAO dao, ILogger<UserBannedEventHandler> logger) 
            : base(dao, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = GetUserId(root);
            _logger.LogInformation("Processing user banned event: {UserId}", userId);

            var existing = await _dao.GetByIdAsync(userId);
            if (existing is null)
            {
                _logger.LogWarning("User not found in MongoDB for banned event: {UserId}", userId);
                return;
            }

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;

            await _dao.UpsertAsync(existing);
            _logger.LogInformation("User marked as BANNED in MongoDB: {UserId}", userId);
        }
    }
}
