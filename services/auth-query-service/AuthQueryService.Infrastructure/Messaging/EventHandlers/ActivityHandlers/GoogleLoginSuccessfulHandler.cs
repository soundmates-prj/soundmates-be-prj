using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.ActivityHandlers
{
    public sealed class GoogleLoginSuccessfulHandler : ActivityEventHandlerBase
    {
        public override string EventType => "auth.user.google.login.successful";

        public GoogleLoginSuccessfulHandler(IUserActivityLogRepository repository, ILogger<GoogleLoginSuccessfulHandler> logger) 
            : base(repository, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = EventPropertyExtractor.GetNullableGuidProperty(root, "id", "Id");
            var username = EventPropertyExtractor.GetOptionalStringProperty(root, "username", "Username");
            var email = EventPropertyExtractor.GetOptionalStringProperty(root, "email", "Email");

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
                Username = username,
                Email = email,
                ActivityType = "google_login",
                EventType = EventType,
                IsSuccess = true,
                OccurredAt = occurredAt
            };

            var metadata = new Dictionary<string, object>
            {
                ["provider"] = "Google"
            };
            log.Metadata = metadata;

            await _repository.CreateAsync(log);
            _logger.LogDebug("Logged successful Google login for user: {Email} (UserId: {UserId})", email, userId);
        }
    }
}
