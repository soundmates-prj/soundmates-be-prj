using System.Text.Json;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers
{
    /// <summary>
    /// Base class for user event handlers with common functionality
    /// </summary>
    public abstract class UserEventHandlerBase : IUserEventHandler
    {
        protected readonly IUserReadRepository _repository;
        protected readonly ILogger _logger;

        public abstract string EventType { get; }

        protected UserEventHandlerBase(IUserReadRepository repository, ILogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task HandleAsync(JsonDocument eventData, CancellationToken cancellationToken)
        {
            try
            {
                var root = eventData.RootElement;
                await HandleEventAsync(root, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle event: {EventType}", EventType);
                throw;
            }
        }

        protected abstract Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken);

        protected Guid GetUserId(JsonElement root)
        {
            return EventPropertyExtractor.GetGuidProperty(root, "id", "Id", "userId", "UserId");
        }
    }
}
