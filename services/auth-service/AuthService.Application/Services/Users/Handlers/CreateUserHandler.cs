using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Users.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services.Users.Handlers;

public sealed class CreateUserHandler(
    IUserRepository repo, 
    IOutbox outbox,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<ApiResponse<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Validate duplicate username
        var existingByUsername = await repo.GetByUsernameAsync(command.Username);
        if (existingByUsername != null)
        {
            return ApiResponse<Guid>.FailureResponse($"Username '{command.Username}' is already taken", 400);
        }

        // Validate duplicate email
        var existingByEmail = await repo.GetByEmailAsync(command.Email);
        if (existingByEmail != null)
        {
            return ApiResponse<Guid>.FailureResponse($"Email '{command.Email}' is already registered", 400);
        }

        // Hash password if provided
        var passwordHash = !string.IsNullOrWhiteSpace(command.Password)
            ? BCrypt.Net.BCrypt.HashPassword(command.Password)
            : string.Empty;

        // Use domain factory method to create user
        User user;
        try
        {
            user = User.CreateAdminUser(
                command.Username,
                command.Email,
                passwordHash,
                command.FirstName,
                command.LastName,
                command.RoleId,
                dateTimeProvider);
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<Guid>.FailureResponse(ex.Message, 400);
        }

        await repo.AddAsync(user);

        // Reload user with role to get role name
        user = await repo.GetByIdAsync(user.Id);

        await outbox.EnqueueAsync("auth.user.created", new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            roleId = user.RoleId,
            roleName = user.Role?.Name,
            isActive = user.IsActive,
            createdAt = user.CreatedAt
        }, cancellationToken);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return ApiResponse<Guid>.SuccessResponse(user.Id, $"Create User {fullName} Successfully!");
    }
}