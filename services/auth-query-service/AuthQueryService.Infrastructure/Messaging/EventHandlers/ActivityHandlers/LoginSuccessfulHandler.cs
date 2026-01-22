using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Infrastructure.DAO.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.ActivityHandlers
{
    public sealed class LoginSuccessfulHandler : ActivityEventHandlerBase
    {
        public override string EventType => "auth.user.login.successful";

        public LoginSuccessfulHandler(IUserActivityLogDAO dao, ILogger<LoginSuccessfulHandler> logger) 
            : base(dao, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            // Extract user data from nested "user" object
            JsonElement userData = root;
            if (root.TryGetProperty("user", out var userProp))
            {
                userData = userProp;
            }

            var userId = EventPropertyExtractor.GetNullableGuidProperty(userData, "id", "Id");
            var username = EventPropertyExtractor.GetOptionalStringProperty(userData, "username", "Username");
            var email = EventPropertyExtractor.GetOptionalStringProperty(userData, "email", "Email");

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
                ActivityType = "login",
                EventType = EventType,
                IsSuccess = true,
                OccurredAt = occurredAt
            };

            // Try to extract additional metadata
            var metadata = new Dictionary<string, object>();
            
            if (root.TryGetProperty("ipAddress", out var ip) && ip.ValueKind == JsonValueKind.String)
                metadata["ipAddress"] = ip.GetString()!;
            
            if (root.TryGetProperty("userAgent", out var ua) && ua.ValueKind == JsonValueKind.String)
                metadata["userAgent"] = ua.GetString()!;

            if (metadata.Count > 0)
                log.Metadata = metadata;

            await _dao.CreateAsync(log);
            _logger.LogDebug("Logged successful login for user: {Username} (UserId: {UserId})", username, userId);
        }
    }
}
