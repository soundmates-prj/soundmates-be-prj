using System.Text.Json;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserUpdatedEventHandler : UserEventHandlerBase
    {
        public override string EventType => "auth.user.updated";

        public UserUpdatedEventHandler(IUserReadRepository repository, ILogger<UserUpdatedEventHandler> logger) 
            : base(repository, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = GetUserId(root);
            _logger.LogDebug("Updating user in MongoDB: {UserId}", userId);

            var existing = await _repository.GetByIdAsync(userId);
            if (existing is null)
            {
                _logger.LogWarning("User not found in MongoDB for update: {UserId}", userId);
                return;
            }

            // Update fields if present
            var username = EventPropertyExtractor.GetOptionalStringProperty(root, "username", "Username");
            var email = EventPropertyExtractor.GetOptionalStringProperty(root, "email", "Email");
            var (firstName, lastName) = EventPropertyExtractor.ParseName(root);
            var roleId = EventPropertyExtractor.GetNullableGuidProperty(root, "roleId", "RoleId");
            var roleName = EventPropertyExtractor.GetOptionalStringProperty(root, "roleName", "RoleName");

            if (!string.IsNullOrEmpty(username)) existing.Username = username;
            if (!string.IsNullOrEmpty(email)) existing.Email = email;
            if (!string.IsNullOrEmpty(firstName)) existing.FirstName = firstName;
            if (!string.IsNullOrEmpty(lastName)) existing.LastName = lastName;
            if (roleId.HasValue) existing.RoleId = roleId;
            if (!string.IsNullOrEmpty(roleName)) existing.RoleName = roleName;

            // Handle IsActive if present
            if (root.TryGetProperty("isActive", out var isActiveProp) || 
                root.TryGetProperty("IsActive", out isActiveProp))
            {
                existing.IsActive = isActiveProp.ValueKind == JsonValueKind.True;
            }

            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpsertAsync(existing);
            _logger.LogDebug("User updated in MongoDB: {UserId}", userId);
        }
    }
}
