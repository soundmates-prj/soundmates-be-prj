using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.ActivityHandlers
{
    public sealed class LoginFailedHandler : ActivityEventHandlerBase
    {
        public override string EventType => "auth.user.login.failed";

        public LoginFailedHandler(IUserActivityLogRepository repository, ILogger<LoginFailedHandler> logger) 
            : base(repository, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = EventPropertyExtractor.GetNullableGuidProperty(root, "userId", "UserId");
            var emailOrUsername = EventPropertyExtractor.GetOptionalStringProperty(root, "emailOrUsername", "EmailOrUsername");
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

            // Determine if emailOrUsername is an email or username
            bool isEmail = !string.IsNullOrEmpty(emailOrUsername) && emailOrUsername.Contains('@');

            var log = new UserActivityLog
            {
                UserId = userId,
                Email = isEmail ? emailOrUsername : null,
                Username = !isEmail ? emailOrUsername : null,
                ActivityType = "login",
                EventType = EventType,
                IsSuccess = false,
                Reason = reason,
                ErrorCode = errorCode,
                OccurredAt = occurredAt
            };

            await _repository.CreateAsync(log);
            _logger.LogDebug("Logged failed login attempt for: {EmailOrUsername} (UserId: {UserId})", emailOrUsername, userId?.ToString() ?? "N/A");
        }
    }
}
