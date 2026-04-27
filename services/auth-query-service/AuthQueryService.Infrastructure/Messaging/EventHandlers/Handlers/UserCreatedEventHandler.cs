using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserCreatedEventHandler : UserEventHandlerBase
    {
        public override string EventType => "auth.user.created";

        public UserCreatedEventHandler(IUserReadRepository repository, ILogger<UserCreatedEventHandler> logger) 
            : base(repository, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userData = EventPropertyExtractor.GetDataElement(root);

            var id = EventPropertyExtractor.GetGuidProperty(userData, "id", "Id", "userId", "UserId");
            var username = EventPropertyExtractor.GetStringProperty(userData, "username", "Username", "userName", "UserName");
            var email = EventPropertyExtractor.GetStringProperty(userData, "email", "Email");
            var (firstName, lastName) = EventPropertyExtractor.ParseName(userData);
            var roleId = EventPropertyExtractor.GetNullableGuidProperty(userData, "roleId", "RoleId", "role_id");
            var roleName = EventPropertyExtractor.GetStringProperty(userData, "roleName", "RoleName", "role_name");
            var isActive = EventPropertyExtractor.GetBooleanProperty(userData, "isActive", "IsActive");
            var isVerified = EventPropertyExtractor.GetBooleanProperty(userData, "isVerified", "IsVerified");
            var emailVerifiedAt = EventPropertyExtractor.GetDateTimeProperty(userData, "emailVerifiedAt", "EmailVerifiedAt");

            // Extract canonical accountStatus (1=Active, 2=Deactivated, 3=Suspended, 4=PendingDeletion)
            // Fall back to isActive → AccountStatus mapping for legacy events without accountStatus field
            int accountStatus = GetAccountStatus(userData, isActive);

            _logger.LogDebug("Creating user in MongoDB: {Username} ({Id})", username, id);

            // Kiểm tra xem User đã được tạo bởi một sự kiện cập nhật đến trước hay chưa
            var existing = await _repository.GetByIdAsync(id);
            if (existing != null)
            {
                _logger.LogInformation("User {Id} already exists in MongoDB read-model (arrival out of order). Skipping creation.", id);
                return;
            }

            var userModel = new UserReadModel
            {
                Id = id,
                Username = username,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                RoleId = roleId,
                RoleName = roleName,
                IsActive = isActive,
                AccountStatus = accountStatus,
                IsVerified = isVerified,
                EmailVerifiedAt = emailVerifiedAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.UpsertAsync(userModel);
            _logger.LogInformation("User created in MongoDB: {Username} ({Id})", username, id);
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
