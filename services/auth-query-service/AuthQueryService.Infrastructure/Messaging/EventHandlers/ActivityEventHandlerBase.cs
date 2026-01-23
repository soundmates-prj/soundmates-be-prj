using System.Text.Json;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers
{
    /// <summary>
    /// Base class for activity event handlers with common functionality
    /// </summary>
    public abstract class ActivityEventHandlerBase : IActivityEventHandler
    {
        protected readonly IUserActivityLogRepository _repository;
        protected readonly ILogger _logger;

        public abstract string EventType { get; }

        protected ActivityEventHandlerBase(IUserActivityLogRepository repository, ILogger logger)
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
                _logger.LogError(ex, "Failed to handle activity event: {EventType}", EventType);
                throw;
            }
        }

        protected abstract Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken);

        protected Guid? GetOptionalUserId(JsonElement root)
        {
            return EventPropertyExtractor.GetNullableGuidProperty(root, "userId", "UserId", "id", "Id");
        }
    }
}
