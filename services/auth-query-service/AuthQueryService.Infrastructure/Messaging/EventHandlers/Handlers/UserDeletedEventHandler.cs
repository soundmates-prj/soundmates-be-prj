using System.Text.Json;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserDeletedEventHandler : UserEventHandlerBase
    {
        public override string EventType => "auth.user.deleted";

        public UserDeletedEventHandler(IUserReadRepository repository, ILogger<UserDeletedEventHandler> logger) 
            : base(repository, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = GetUserId(root);
            _logger.LogDebug("Deleting user from MongoDB: {UserId}", userId);

            await _repository.DeleteAsync(userId);
            _logger.LogDebug("User deleted from MongoDB: {UserId}", userId);
        }
    }
}
