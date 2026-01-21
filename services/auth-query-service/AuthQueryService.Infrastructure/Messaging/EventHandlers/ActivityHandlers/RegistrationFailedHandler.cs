using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Infrastructure.DAO.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.ActivityHandlers
{
    public sealed class RegistrationFailedHandler : ActivityEventHandlerBase
    {
        public override string EventType => "auth.user.registration.failed";

        public RegistrationFailedHandler(IUserActivityLogDAO dao, ILogger<RegistrationFailedHandler> logger) 
            : base(dao, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var username = EventPropertyExtractor.GetOptionalStringProperty(root, "username", "Username");
            var email = EventPropertyExtractor.GetOptionalStringProperty(root, "email", "Email");
            var reason = EventPropertyExtractor.GetOptionalStringProperty(root, "reason", "Reason") ?? "Unknown";
            
            var errorCode = 400;
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
                Username = username,
                Email = email,
                ActivityType = "registration",
                EventType = EventType,
                IsSuccess = false,
                Reason = reason,
                ErrorCode = errorCode,
                OccurredAt = occurredAt
            };

            await _dao.CreateAsync(log);
            _logger.LogDebug("Logged failed registration for: {Email}", email);
        }
    }
}
