using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Infrastructure.DAO.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserCreatedEventHandler : UserEventHandlerBase
    {
        public override string EventType => "auth.user.created";

        public UserCreatedEventHandler(IUserReadDAO dao, ILogger<UserCreatedEventHandler> logger) 
            : base(dao, logger) { }

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

            _logger.LogDebug("Creating user in MongoDB: {Username} ({Id})", username, id);

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dao.UpsertAsync(userModel);
            _logger.LogDebug("User created in MongoDB: {Username}", username);
        }
    }
}
