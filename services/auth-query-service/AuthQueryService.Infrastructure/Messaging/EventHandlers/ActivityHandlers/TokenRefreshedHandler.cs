using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Infrastructure.DAO.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.ActivityHandlers
{
    public sealed class TokenRefreshedHandler : ActivityEventHandlerBase
    {
        public override string EventType => "auth.user.token.refreshed";

        public TokenRefreshedHandler(IUserActivityLogDAO dao, ILogger<TokenRefreshedHandler> logger) 
            : base(dao, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = EventPropertyExtractor.GetNullableGuidProperty(root, "id", "Id");

            // Try to parse occurredAtUtc from event
            DateTime occurredAt = DateTime.UtcNow;
            if (root.TryGetProperty("occurredAtUtc", out var occurredProp) && occurredProp.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(occurredProp.GetString(), out var parsed))
                    occurredAt = parsed;
            }

            var log = new UserActivityLog
            {
                UserId = userId,
                ActivityType = "token_refresh",
                EventType = EventType,
                IsSuccess = true,
                OccurredAt = occurredAt
            };

            await _dao.CreateAsync(log);
            _logger.LogDebug("Logged token refresh for user: {UserId}", userId);
        }
    }
}
