using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Infrastructure.DAO.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.ActivityHandlers
{
    public sealed class LoginActivityHandler : ActivityEventHandlerBase
    {
        public override string EventType => "auth.user.login.activity";

        public LoginActivityHandler(IUserActivityLogDAO dao, ILogger<LoginActivityHandler> logger) 
            : base(dao, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = EventPropertyExtractor.GetNullableGuidProperty(root, "userId", "UserId");
            var username = EventPropertyExtractor.GetOptionalStringProperty(root, "username", "Username");
            var email = EventPropertyExtractor.GetOptionalStringProperty(root, "email", "Email");
            var ipAddress = EventPropertyExtractor.GetOptionalStringProperty(root, "ipAddress", "IpAddress");
            var userAgent = EventPropertyExtractor.GetOptionalStringProperty(root, "userAgent", "UserAgent");
            var location = EventPropertyExtractor.GetOptionalStringProperty(root, "location", "Location");

            var log = new UserActivityLog
            {
                UserId = userId,
                Username = username,
                Email = email,
                ActivityType = "login_activity",
                EventType = EventType,
                IsSuccess = true,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Location = location,
                OccurredAt = DateTime.UtcNow
            };

            // Extract additional metadata
            var metadata = new Dictionary<string, object>();
            
            if (root.TryGetProperty("deviceType", out var deviceType) && deviceType.ValueKind == JsonValueKind.String)
                metadata["deviceType"] = deviceType.GetString()!;
            
            if (root.TryGetProperty("browser", out var browser) && browser.ValueKind == JsonValueKind.String)
                metadata["browser"] = browser.GetString()!;

            if (metadata.Count > 0)
                log.Metadata = metadata;

            await _dao.CreateAsync(log);
            _logger.LogDebug("Logged login activity for user: {Username}", username);
        }
    }
}
