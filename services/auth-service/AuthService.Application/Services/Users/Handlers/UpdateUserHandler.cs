using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Users.Commands;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services.Users.Handlers;

public sealed class UpdateUserHandler(IUserRepository repo, IOutbox outbox) : ICommandHandler<UpdateUserCommand, bool>
{
    public async Task<ApiResponse<bool>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await repo.GetByIdAsync(command.Id);
        if (user is null) return ApiResponse<bool>.FailureResponse("User not found", 404);

        // Validate duplicate username (if changed)
        if (user.Username != command.Username)
        {
            var existingByUsername = await repo.GetByUsernameAsync(command.Username);
            if (existingByUsername != null && existingByUsername.Id != command.Id)
            {
                return ApiResponse<bool>.FailureResponse($"Username '{command.Username}' is already taken", 400);
            }
        }

        // Validate duplicate email (if changed)
        if (user.Email != command.Email)
        {
            var existingByEmail = await repo.GetByEmailAsync(command.Email);
            if (existingByEmail != null && existingByEmail.Id != command.Id)
            {
                return ApiResponse<bool>.FailureResponse($"Email '{command.Email}' is already registered", 400);
            }
        }

        user.Username = command.Username;
        user.Email = command.Email;
        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        user.RoleId = command.RoleId;
        user.UpdatedAt = DateTime.UtcNow;

        await repo.UpdateAsync(user);

        // Reload user with role to get role name
        user = await repo.GetByIdAsync(user.Id);

        await outbox.EnqueueAsync("auth.user.updated", new
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

        return ApiResponse<bool>.SuccessResponse(true, "Update User Successfully!");
    }
}