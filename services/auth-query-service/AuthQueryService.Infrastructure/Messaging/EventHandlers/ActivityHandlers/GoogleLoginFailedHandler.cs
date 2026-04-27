using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.ActivityHandlers
{
    public sealed class GoogleLoginFailedHandler : ActivityEventHandlerBase
    {
        public override string EventType => "auth.user.google.login.failed";

        public GoogleLoginFailedHandler(IUserActivityLogRepository repository, ILogger<GoogleLoginFailedHandler> logger) 
            : base(repository, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var reason = EventPropertyExtractor.GetOptionalStringProperty(root, "reason", "Reason") ?? "Unknown";
            
            var errorCode = 401;
            if (root.TryGetProperty("errorCode", out var errorCodeProp) && errorCodeProp.ValueKind == JsonValueKind.Number)
            {
                errorCode = errorCodeProp.GetInt32();
            }

            // Try to parse occurredAtUtc from event
            DateTime occurredAt = DateTime.UtcNow;
            if (root.TryGetProperty("occurredAtUtc", out var occurredProp) && occurredProp.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(occurredProp.GetString(), out var parsed))
                    occurredAt = parsed;
            }

            var log = new UserActivityLog
            {
                ActivityType = "google_login",
                EventType = EventType,
                IsSuccess = false,
                Reason = reason,
                ErrorCode = errorCode,
                OccurredAt = occurredAt
            };

            var metadata = new Dictionary<string, object>
            {
                ["provider"] = "Google"
            };
            log.Metadata = metadata;

            await _repository.CreateAsync(log);
            _logger.LogDebug("Logged failed Google login attempt: {Reason}", reason);
        }
    }
}
