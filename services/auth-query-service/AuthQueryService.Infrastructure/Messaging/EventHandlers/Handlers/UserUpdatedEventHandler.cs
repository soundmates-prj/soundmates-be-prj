using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using AuthQueryService.Infrastructure.Messaging.EventHandlers;
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
            var userData = EventPropertyExtractor.GetDataElement(root);
            var userId = GetUserId(userData);

            _logger.LogDebug("Processing user update for {UserId}", userId);

            var isActive = EventPropertyExtractor.GetBooleanProperty(userData, "isActive", "IsActive");
            var existing = await _repository.GetByIdAsync(userId);
            bool isNew = false;

            if (existing is null)
            {
                _logger.LogWarning("User not found in MongoDB for update: {UserId}. Creating new entry (Upsert).", userId);
                isNew = true;

                // Khởi tạo model mới nếu chưa tồn tại (Xử lý trường hợp sự kiện Created đến sau)
                existing = new UserReadModel
                {
                    Id = userId,
                    Username = EventPropertyExtractor.GetStringProperty(userData, "username", "Username"),
                    Email = EventPropertyExtractor.GetStringProperty(userData, "email", "Email"),
                    FirstName = EventPropertyExtractor.GetOptionalStringProperty(userData, "firstName", "FirstName"),
                    LastName = EventPropertyExtractor.GetOptionalStringProperty(userData, "lastName", "LastName"),
                    RoleId = EventPropertyExtractor.GetNullableGuidProperty(userData, "roleId", "RoleId"),
                    RoleName = EventPropertyExtractor.GetOptionalStringProperty(userData, "roleName", "RoleName"),
                    IsActive = isActive,
                    AccountStatus = GetAccountStatus(userData, isActive),
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Cập nhật các trường nếu có trong event
            var username = EventPropertyExtractor.GetOptionalStringProperty(userData, "username", "Username");
            var email = EventPropertyExtractor.GetOptionalStringProperty(userData, "email", "Email");
            var (firstName, lastName) = EventPropertyExtractor.ParseName(userData);
            var roleId = EventPropertyExtractor.GetNullableGuidProperty(userData, "roleId", "RoleId");
            var roleName = EventPropertyExtractor.GetOptionalStringProperty(userData, "roleName", "RoleName");

            if (!string.IsNullOrEmpty(username)) existing.Username = username;
            if (!string.IsNullOrEmpty(email)) existing.Email = email;
            if (!string.IsNullOrEmpty(firstName)) existing.FirstName = firstName;
            if (!string.IsNullOrEmpty(lastName)) existing.LastName = lastName;
            if (roleId.HasValue) existing.RoleId = roleId;
            if (!string.IsNullOrEmpty(roleName)) existing.RoleName = roleName;

            // Xử lý IsActive và canonical AccountStatus
            existing.IsActive = isActive;
            existing.AccountStatus = GetAccountStatus(userData, isActive);

            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpsertAsync(existing);
            _logger.LogInformation(isNew ? "User created via Update event: {Id}" : "User updated in MongoDB: {Id}", userId);
        }

        private static int GetAccountStatus(JsonElement userData, bool isActive)
        {
            // Try to read accountStatus directly from event
            if (userData.TryGetProperty("accountStatus", out var statusProp) &&
                statusProp.ValueKind == JsonValueKind.Number)
            {
                return statusProp.GetInt32();
            }
            // Fallback: map isActive to canonical status
            return isActive ? 1 : 2;
        }
    }
}
