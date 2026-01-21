using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Auth.Commands;
using AuthService.Domain.Interfaces;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Services.Auth.Handlers
{
    public sealed class UpdateProfileHandler : ICommandHandler<UpdateProfileCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOutbox _outbox;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateProfileHandler(
            IUserRepository userRepository,
            IOutbox outbox,
            IDateTimeProvider dateTimeProvider)
        {
            _userRepository = userRepository;
            _outbox = outbox;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<ApiResponse<UserDto>> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
        {
            // Get user
            var user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
            {
                return ApiResponse<UserDto>.FailureResponse("User not found", 404);
            }

            // Use domain method for updating name
            try
            {
                user.UpdateName(
                    command.FirstName,
                    command.LastName,
                    _dateTimeProvider);
            }
            catch (ArgumentException ex)
            {
                return ApiResponse<UserDto>.FailureResponse(ex.Message, 400);
            }

            await _userRepository.UpdateAsync(user);

            // Reload user with role
            user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
            {
                return ApiResponse<UserDto>.FailureResponse("Failed to retrieve updated user", 500);
            }

            var dto = new UserDto(user);

            // Publish event - use auth.user.updated to sync to query service
            await _outbox.EnqueueAsync("auth.user.updated", new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                roleId = user.RoleId,
                roleName = user.Role?.Name,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt
            }, cancellationToken);

            return ApiResponse<UserDto>.SuccessResponse(dto, "Profile updated successfully");
        }
    }
}

