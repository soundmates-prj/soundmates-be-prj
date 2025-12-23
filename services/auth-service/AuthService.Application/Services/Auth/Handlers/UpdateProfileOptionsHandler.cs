using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Services.Auth.Handlers
{
    public sealed class UpdateProfileOptionsHandler : ICommandHandler<UpdateProfileOptionsCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IOutbox _outbox;

        public UpdateProfileOptionsHandler(
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            IOutbox outbox)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _outbox = outbox;
        }

        public async Task<ApiResponse<UserDto>> Handle(UpdateProfileOptionsCommand command, CancellationToken cancellationToken)
        {
            // Get user
            var user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
            {
                return ApiResponse<UserDto>.FailureResponse("User not found", 404);
            }

            // Get or create profile
            var profile = await _profileRepository.GetByUserIdAsync(command.UserId);
            if (profile == null)
            {
                profile = new Profile
                {
                    Id = Guid.NewGuid(),
                    UserId = command.UserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _profileRepository.CreateAsync(profile);
            }

            // Update profile fields (only if provided)
            if (command.Bio != null)
                profile.Bio = command.Bio.Trim();
            
            if (command.Phone != null)
                profile.Phone = command.Phone.Trim();
            
            if (command.Gender != null)
                profile.Gender = command.Gender.Trim();
            
            if (command.DateOfBirth.HasValue)
                profile.DateOfBirth = command.DateOfBirth.Value;
            
            if (command.ProfileImageUrl != null)
                profile.ProfileImageUrl = command.ProfileImageUrl.Trim();
            
            if (command.BackgroundImageUrl != null)
                profile.BackgroundImageUrl = command.BackgroundImageUrl.Trim();
            
            if (command.Location != null)
                profile.Location = command.Location.Trim();
            
            if (command.Website != null)
                profile.Website = command.Website.Trim();

            profile.UpdatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _profileRepository.UpdateAsync(profile);
            await _userRepository.UpdateAsync(user);

            // Reload user with role
            user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
            {
                return ApiResponse<UserDto>.FailureResponse("Failed to retrieve updated user", 500);
            }

            // Reload profile to get latest data
            profile = await _profileRepository.GetByUserIdAsync(command.UserId);
            if (profile == null)
            {
                return ApiResponse<UserDto>.FailureResponse("Failed to retrieve updated profile", 500);
            }

            var dto = new UserDto(user);

            // Publish event - sync profile data to query service
            await _outbox.EnqueueAsync("auth.user.profile.updated", new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                roleId = user.RoleId,
                roleName = user.Role?.Name,
                profile = new
                {
                    bio = profile.Bio,
                    phone = profile.Phone,
                    gender = profile.Gender,
                    dateOfBirth = profile.DateOfBirth,
                    profileImageUrl = profile.ProfileImageUrl,
                    backgroundImageUrl = profile.BackgroundImageUrl,
                    location = profile.Location,
                    website = profile.Website
                },
                updatedAt = user.UpdatedAt
            }, cancellationToken);

            return ApiResponse<UserDto>.SuccessResponse(dto, "Profile options updated successfully");
        }
    }
}

