using System.Text.Json;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers
{
    public sealed class UserProfileUpdatedEventHandler : UserEventHandlerBase
    {
        public override string EventType => "auth.user.profile.updated";

        public UserProfileUpdatedEventHandler(IUserReadRepository repository, ILogger<UserProfileUpdatedEventHandler> logger) 
            : base(repository, logger) { }

        protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
        {
            var userId = GetUserId(root);
            _logger.LogDebug("Updating user profile in MongoDB: {UserId}", userId);

            var existing = await _repository.GetByIdAsync(userId);
            if (existing is null)
            {
                _logger.LogWarning("User not found in MongoDB for profile update: {UserId}. Creating new entry.", userId);
                
                // If user doesn't exist, create new entry with profile data
                existing = new UserReadModel
                {
                    Id = userId,
                    Username = EventPropertyExtractor.GetStringProperty(root, "username", "Username"),
                    Email = EventPropertyExtractor.GetStringProperty(root, "email", "Email"),
                    FirstName = EventPropertyExtractor.GetOptionalStringProperty(root, "firstName", "FirstName"),
                    LastName = EventPropertyExtractor.GetOptionalStringProperty(root, "lastName", "LastName"),
                    RoleId = EventPropertyExtractor.GetNullableGuidProperty(root, "roleId", "RoleId"),
                    RoleName = EventPropertyExtractor.GetOptionalStringProperty(root, "roleName", "RoleName"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Update basic user fields if present
            var username = EventPropertyExtractor.GetOptionalStringProperty(root, "username", "Username");
            var email = EventPropertyExtractor.GetOptionalStringProperty(root, "email", "Email");
            var firstName = EventPropertyExtractor.GetOptionalStringProperty(root, "firstName", "FirstName");
            var lastName = EventPropertyExtractor.GetOptionalStringProperty(root, "lastName", "LastName");
            var roleId = EventPropertyExtractor.GetNullableGuidProperty(root, "roleId", "RoleId");
            var roleName = EventPropertyExtractor.GetOptionalStringProperty(root, "roleName", "RoleName");

            if (!string.IsNullOrEmpty(username)) existing.Username = username;
            if (!string.IsNullOrEmpty(email)) existing.Email = email;
            if (!string.IsNullOrEmpty(firstName)) existing.FirstName = firstName;
            if (!string.IsNullOrEmpty(lastName)) existing.LastName = lastName;
            if (roleId.HasValue) existing.RoleId = roleId;
            if (!string.IsNullOrEmpty(roleName)) existing.RoleName = roleName;

            // Extract profile data from nested profile object
            if (root.TryGetProperty("profile", out var profileElement))
            {
                existing.Bio = EventPropertyExtractor.GetOptionalStringProperty(profileElement, "bio", "Bio");
                existing.Phone = EventPropertyExtractor.GetOptionalStringProperty(profileElement, "phone", "Phone");
                existing.Gender = EventPropertyExtractor.GetOptionalStringProperty(profileElement, "gender", "Gender");
                existing.ProfileImageUrl = EventPropertyExtractor.GetOptionalStringProperty(profileElement, "profileImageUrl", "ProfileImageUrl");
                existing.BackgroundImageUrl = EventPropertyExtractor.GetOptionalStringProperty(profileElement, "backgroundImageUrl", "BackgroundImageUrl");
                existing.Location = EventPropertyExtractor.GetOptionalStringProperty(profileElement, "location", "Location");
                existing.Website = EventPropertyExtractor.GetOptionalStringProperty(profileElement, "website", "Website");

                // Handle DateOfBirth
                if (profileElement.TryGetProperty("dateOfBirth", out var dobProp) || 
                    profileElement.TryGetProperty("DateOfBirth", out dobProp))
                {
                    if (dobProp.ValueKind == JsonValueKind.String && DateTime.TryParse(dobProp.GetString(), out var dob))
                    {
                        existing.DateOfBirth = dob;
                    }
                    else if (dobProp.ValueKind == JsonValueKind.Null)
                    {
                        existing.DateOfBirth = null;
                    }
                }
            }

            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpsertAsync(existing);
            _logger.LogDebug("User profile updated in MongoDB: {UserId}", userId);
        }
    }
}
