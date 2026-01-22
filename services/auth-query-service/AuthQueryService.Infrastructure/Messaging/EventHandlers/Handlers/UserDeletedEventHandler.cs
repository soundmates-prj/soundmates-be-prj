using System.Text.Json;
using AuthQueryService.Infrastructure.DAO.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserDeletedEventHandler : UserEventHandlerBase
    {
        public override string EventType => "auth.user.deleted";

        public UserDeletedEventHandler(IUserReadDAO dao, ILogger<UserDeletedEventHandler> logger) 
            : base(dao, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = GetUserId(root);
            _logger.LogDebug("Deleting user from MongoDB: {UserId}", userId);

            await _dao.DeleteAsync(userId);
            _logger.LogDebug("User deleted from MongoDB: {UserId}", userId);
        }
    }
}
